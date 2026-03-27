package com.sendit.api

import android.content.Context
import android.provider.Settings
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import java.util.concurrent.TimeUnit

// MARK: - Data Models

@Serializable
data class CareerData(
    val level: Int,
    val totalExperience: Int,
    val balance: Double,
    val racesCompleted: Int,
    val wins: Int,
    val bestLapTime: Double
)

@Serializable
data class RaceResult(
    val eventName: String,
    val trackName: String,
    val position: Int,
    val bestLapTime: Double,
    val prizeMoney: Double,
    val experiencePoints: Int,
    val completeDate: String
)

@Serializable
data class VehicleUpgrade(
    val name: String,
    val type: String,
    val cost: Double,
    val performanceGain: Double,
    val requiredLevel: Int
)

@Serializable
data class VehicleSetup(
    val name: String,
    val track: String,
    val bestLapTime: Double,
    val useCount: Int,
    val created: String
)

@Serializable
data class LoginResponse(
    val success: Boolean,
    val sessionToken: String,
    val message: String
)

@Serializable
data class RaceHistoryResponse(
    val success: Boolean,
    val races: List<RaceResult>
)

@Serializable
data class UpgradesResponse(
    val success: Boolean,
    val upgrades: List<VehicleUpgrade>
)

@Serializable
data class SetupsResponse(
    val success: Boolean,
    val setups: List<VehicleSetup>
)

@Serializable
data class PurchaseResponse(
    val success: Boolean,
    val message: String
)

// MARK: - API Client

class SendItAPIClient(
    private val context: Context,
    private val baseURL: String = "http://localhost:8080"
) {
    private val deviceId: String = Settings.Secure.getString(
        context.contentResolver,
        Settings.Secure.ANDROID_ID
    )

    private val httpClient = OkHttpClient.Builder()
        .connectTimeout(10, TimeUnit.SECONDS)
        .readTimeout(10, TimeUnit.SECONDS)
        .writeTimeout(10, TimeUnit.SECONDS)
        .build()

    private val json = Json { ignoreUnknownKeys = true }
    private val dataStore = context.dataStore

    private var sessionToken: String? = null

    init {
        // Try to restore session token
        try {
            val prefs = android.content.SharedPreferences(context)
            sessionToken = prefs.getString("sessionToken", null)
        } catch (e: Exception) {
            // Preference not available, will require login
        }
    }

    suspend fun login(): Result<String> = withContext(Dispatchers.IO) {
        try {
            val loginBody = Json.encodeToString(
                mapOf(
                    "deviceId" to deviceId,
                    "appVersion" to "1.0.0"
                )
            )

            val request = Request.Builder()
                .url("$baseURL/api/login")
                .post(loginBody.toRequestBody("application/json".toMediaType()))
                .header("Content-Type", "application/json")
                .build()

            val response = httpClient.newCall(request).execute()

            if (!response.isSuccessful) {
                return@withContext Result.failure(Exception("Login failed"))
            }

            val responseBody = response.body?.string() ?: throw Exception("Empty response")
            val loginResponse = json.decodeFromString<LoginResponse>(responseBody)

            sessionToken = loginResponse.sessionToken
            // Save session token to shared preferences
            saveSessionToken(loginResponse.sessionToken)

            Result.success(loginResponse.sessionToken)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    fun logout() {
        sessionToken = null
        clearSessionToken()
    }

    suspend fun fetchCareerData(): Result<CareerData> = withContext(Dispatchers.IO) {
        try {
            val response = makeAuthenticatedRequest("/api/career", "GET")
            if (!response.isSuccessful) {
                return@withContext Result.failure(Exception("Failed to fetch career data"))
            }

            val responseBody = response.body?.string() ?: throw Exception("Empty response")
            val careerData = json.decodeFromString<CareerData>(responseBody)
            Result.success(careerData)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun fetchRaceHistory(count: Int = 10): Result<List<RaceResult>> = withContext(Dispatchers.IO) {
        try {
            val response = makeAuthenticatedRequest("/api/career/races?count=$count", "GET")
            if (!response.isSuccessful) {
                return@withContext Result.failure(Exception("Failed to fetch race history"))
            }

            val responseBody = response.body?.string() ?: throw Exception("Empty response")
            val historyResponse = json.decodeFromString<RaceHistoryResponse>(responseBody)
            Result.success(historyResponse.races)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun fetchAvailableUpgrades(): Result<List<VehicleUpgrade>> = withContext(Dispatchers.IO) {
        try {
            val response = makeAuthenticatedRequest("/api/upgrades/available", "GET")
            if (!response.isSuccessful) {
                return@withContext Result.failure(Exception("Failed to fetch upgrades"))
            }

            val responseBody = response.body?.string() ?: throw Exception("Empty response")
            val upgradesResponse = json.decodeFromString<UpgradesResponse>(responseBody)
            Result.success(upgradesResponse.upgrades)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun purchaseUpgrade(upgradeName: String): Result<Boolean> = withContext(Dispatchers.IO) {
        try {
            val body = Json.encodeToString(mapOf("upgradeName" to upgradeName))
            val response = makeAuthenticatedRequest(
                "/api/upgrades/purchase",
                "POST",
                body
            )

            if (!response.isSuccessful) {
                return@withContext Result.failure(Exception("Purchase failed"))
            }

            val responseBody = response.body?.string() ?: throw Exception("Empty response")
            val purchaseResponse = json.decodeFromString<PurchaseResponse>(responseBody)
            Result.success(purchaseResponse.success)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun fetchSavedSetups(): Result<List<VehicleSetup>> = withContext(Dispatchers.IO) {
        try {
            val response = makeAuthenticatedRequest("/api/setups", "GET")
            if (!response.isSuccessful) {
                return@withContext Result.failure(Exception("Failed to fetch setups"))
            }

            val responseBody = response.body?.string() ?: throw Exception("Empty response")
            val setupsResponse = json.decodeFromString<SetupsResponse>(responseBody)
            Result.success(setupsResponse.setups)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun loadSetup(setupName: String): Result<Boolean> = withContext(Dispatchers.IO) {
        try {
            val body = Json.encodeToString(mapOf("setupName" to setupName))
            val response = makeAuthenticatedRequest(
                "/api/setups/load",
                "POST",
                body
            )

            if (!response.isSuccessful) {
                return@withContext Result.failure(Exception("Load failed"))
            }

            val responseBody = response.body?.string() ?: throw Exception("Empty response")
            val setupResponse = json.decodeFromString<PurchaseResponse>(responseBody)
            Result.success(setupResponse.success)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    fun isAuthenticated(): Boolean = sessionToken != null

    // MARK: - Private Helpers

    private fun makeAuthenticatedRequest(
        endpoint: String,
        method: String,
        body: String? = null
    ): okhttp3.Response {
        if (sessionToken == null) {
            throw Exception("Not authenticated")
        }

        val builder = Request.Builder()
            .url("$baseURL$endpoint")
            .header("Authorization", "Bearer $sessionToken")
            .header("Content-Type", "application/json")

        when (method) {
            "GET" -> builder.get()
            "POST" -> {
                val requestBody = body?.toRequestBody("application/json".toMediaType())
                builder.post(requestBody ?: "{}".toRequestBody("application/json".toMediaType()))
            }
        }

        val request = builder.build()
        return httpClient.newCall(request).execute()
    }

    private fun saveSessionToken(token: String) {
        try {
            val sharedPref = context.getSharedPreferences("SendIt", Context.MODE_PRIVATE)
            sharedPref.edit().putString("sessionToken", token).apply()
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    private fun clearSessionToken() {
        try {
            val sharedPref = context.getSharedPreferences("SendIt", Context.MODE_PRIVATE)
            sharedPref.edit().remove("sessionToken").apply()
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }
}

// MARK: - Extension Functions

fun Double.formatMoney(): String {
    return "$%.0f".format(this)
}

fun Double.formatTime(): String {
    val minutes = this.toInt() / 60
    val seconds = this % 60
    return "%d:%06.3f".format(minutes, seconds)
}

fun Double.formatLapTime(): String {
    return "%.3f".format(this)
}
