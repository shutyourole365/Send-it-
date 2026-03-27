import Foundation

// MARK: - Data Models

struct CareerData: Codable {
    let level: Int
    let totalExperience: Int
    let balance: Double
    let racesCompleted: Int
    let wins: Int
    let bestLapTime: Double
}

struct RaceResult: Codable {
    let eventName: String
    let trackName: String
    let position: Int
    let bestLapTime: Double
    let prizeMoney: Double
    let experiencePoints: Int
    let completeDate: String
}

struct VehicleUpgrade: Codable, Identifiable {
    let id = UUID()
    let name: String
    let type: String
    let cost: Double
    let performanceGain: Double
    let requiredLevel: Int

    enum CodingKeys: String, CodingKey {
        case name, type, cost, performanceGain, requiredLevel
    }
}

struct VehicleSetup: Codable, Identifiable {
    let id = UUID()
    let name: String
    let track: String
    let bestLapTime: Double
    let useCount: Int
    let created: String

    enum CodingKeys: String, CodingKey {
        case name, track, bestLapTime, useCount, created
    }
}

struct LoginResponse: Codable {
    let success: Bool
    let sessionToken: String
    let message: String
}

struct APIResponse<T: Codable>: Codable {
    let success: Bool
    let error: String?
    let data: T?
}

// MARK: - API Client

class SendItAPIClient: ObservableObject {
    @Published var careerData: CareerData?
    @Published var isAuthenticated = false
    @Published var isLoading = false
    @Published var errorMessage: String?

    private let baseURL: URL
    private var sessionToken: String?
    private let deviceId: String
    private let session: URLSession

    init(baseURL: String = "http://localhost:8080", deviceId: String = UIDevice.current.identifierForVendor?.uuidString ?? UUID().uuidString) {
        self.baseURL = URL(string: baseURL)!
        self.deviceId = deviceId

        let config = URLSessionConfiguration.default
        config.timeoutIntervalForRequest = 10
        config.timeoutIntervalForResource = 30
        self.session = URLSession(configuration: config)

        // Restore session token if available
        if let token = UserDefaults.standard.string(forKey: "sessionToken") {
            self.sessionToken = token
            self.isAuthenticated = true
        }
    }

    // MARK: - Authentication

    func login() async throws {
        isLoading = true
        defer { isLoading = false }

        let loginData = ["deviceId": deviceId, "appVersion": "1.0.0"]
        let jsonData = try JSONSerialization.data(withJSONObject: loginData)

        var request = URLRequest(url: baseURL.appendingPathComponent("/api/login"))
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = jsonData

        let (data, response) = try session.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse, httpResponse.statusCode == 200 else {
            throw APIError.invalidResponse
        }

        let loginResponse = try JSONDecoder().decode(LoginResponse.self, from: data)
        self.sessionToken = loginResponse.sessionToken

        // Save session token
        UserDefaults.standard.set(sessionToken, forKey: "sessionToken")

        DispatchQueue.main.async {
            self.isAuthenticated = true
        }
    }

    func logout() {
        sessionToken = nil
        UserDefaults.standard.removeObject(forKey: "sessionToken")

        DispatchQueue.main.async {
            self.isAuthenticated = false
        }
    }

    // MARK: - Career Data

    func fetchCareerData() async throws {
        isLoading = true
        defer { isLoading = false }

        let data = try await makeAuthenticatedRequest(
            endpoint: "/api/career",
            method: "GET",
            responseType: CareerData.self
        )

        DispatchQueue.main.async {
            self.careerData = data
        }
    }

    func fetchRaceHistory(count: Int = 10) async throws -> [RaceResult] {
        let data = try await makeAuthenticatedRequest(
            endpoint: "/api/career/races?count=\(count)",
            method: "GET",
            responseType: RaceHistoryResponse.self
        )
        return data.races
    }

    // MARK: - Vehicle Upgrades

    func fetchAvailableUpgrades() async throws -> [VehicleUpgrade] {
        let data = try await makeAuthenticatedRequest(
            endpoint: "/api/upgrades/available",
            method: "GET",
            responseType: UpgradesResponse.self
        )
        return data.upgrades
    }

    func purchaseUpgrade(name: String) async throws -> Bool {
        let request = ["upgradeName": name]
        let jsonData = try JSONSerialization.data(withJSONObject: request)

        let response = try await makeAuthenticatedJSONRequest(
            endpoint: "/api/upgrades/purchase",
            method: "POST",
            body: jsonData,
            responseType: PurchaseResponse.self
        )

        return response.success
    }

    // MARK: - Vehicle Setups

    func fetchSavedSetups() async throws -> [VehicleSetup] {
        let data = try await makeAuthenticatedRequest(
            endpoint: "/api/setups",
            method: "GET",
            responseType: SetupsResponse.self
        )
        return data.setups
    }

    func loadSetup(name: String) async throws -> Bool {
        let request = ["setupName": name]
        let jsonData = try JSONSerialization.data(withJSONObject: request)

        let response = try await makeAuthenticatedJSONRequest(
            endpoint: "/api/setups/load",
            method: "POST",
            body: jsonData,
            responseType: SetupResponse.self
        )

        return response.success
    }

    // MARK: - Helper Methods

    private func makeAuthenticatedRequest<T: Codable>(
        endpoint: String,
        method: String,
        responseType: T.Type
    ) async throws -> T {
        guard let token = sessionToken else {
            throw APIError.notAuthenticated
        }

        var request = URLRequest(url: baseURL.appendingPathComponent(endpoint))
        request.httpMethod = method
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        let (data, response) = try session.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.invalidResponse
        }

        if httpResponse.statusCode == 401 {
            // Session expired
            self.logout()
            throw APIError.sessionExpired
        }

        guard httpResponse.statusCode == 200 else {
            throw APIError.serverError(httpResponse.statusCode)
        }

        return try JSONDecoder().decode(T.self, from: data)
    }

    private func makeAuthenticatedJSONRequest<T: Codable>(
        endpoint: String,
        method: String,
        body: Data,
        responseType: T.Type
    ) async throws -> T {
        guard let token = sessionToken else {
            throw APIError.notAuthenticated
        }

        var request = URLRequest(url: baseURL.appendingPathComponent(endpoint))
        request.httpMethod = method
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = body

        let (data, response) = try session.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.invalidResponse
        }

        guard httpResponse.statusCode == 200 else {
            throw APIError.serverError(httpResponse.statusCode)
        }

        return try JSONDecoder().decode(T.self, from: data)
    }
}

// MARK: - Supporting Response Types

struct RaceHistoryResponse: Codable {
    let success: Bool
    let races: [RaceResult]
}

struct UpgradesResponse: Codable {
    let success: Bool
    let upgrades: [VehicleUpgrade]
}

struct SetupsResponse: Codable {
    let success: Bool
    let setups: [VehicleSetup]
}

struct PurchaseResponse: Codable {
    let success: Bool
    let message: String
}

struct SetupResponse: Codable {
    let success: Bool
    let message: String
}

// MARK: - Error Types

enum APIError: LocalizedError {
    case invalidResponse
    case notAuthenticated
    case sessionExpired
    case serverError(Int)
    case decodingError
    case networkError(String)

    var errorDescription: String? {
        switch self {
        case .invalidResponse:
            return "Invalid server response"
        case .notAuthenticated:
            return "Not authenticated. Please login first."
        case .sessionExpired:
            return "Your session has expired. Please login again."
        case .serverError(let code):
            return "Server error: \(code)"
        case .decodingError:
            return "Failed to decode response"
        case .networkError(let message):
            return "Network error: \(message)"
        }
    }
}

// MARK: - Utility Extensions

extension Double {
    func formatMoney() -> String {
        let formatter = NumberFormatter()
        formatter.numberStyle = .currency
        formatter.currencySymbol = "$"
        return formatter.string(from: NSNumber(value: self)) ?? "$0"
    }

    func formatTime() -> String {
        let minutes = Int(self) / 60
        let seconds = self.truncatingRemainder(dividingBy: 60)
        return String(format: "%d:%06.3f", minutes, seconds)
    }

    func formatLapTime() -> String {
        return String(format: "%.3f", self)
    }
}
