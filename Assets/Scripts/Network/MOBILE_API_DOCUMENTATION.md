# Enhancement 7: Mobile App Connectivity

## Overview

The Mobile App Connector provides a REST API for remote career management, setup sharing, and vehicle tuning through mobile devices. Players can monitor progress, purchase upgrades, and adjust setups away from the main game.

## Features

### 1. Session Management
- Mobile app login/authentication
- Session token-based security
- 30-minute session timeout
- Device tracking and management

### 2. Career Remote Access
- View current level and experience
- Check balance and earnings
- Review race history
- Track achievements and milestones

### 3. Upgrade Management
- Browse available vehicle upgrades
- Purchase upgrades remotely
- View upgrade requirements and gains
- Track installed modifications

### 4. Setup Sharing
- View saved vehicle setups
- Load setups from mobile app
- Share setup configurations
- Compare setup performance

## API Endpoints

### Authentication

#### POST /api/login
Authenticate mobile app and create session.

**Request:**
```json
{
  "deviceId": "device-uuid",
  "appVersion": "1.0.0"
}
```

**Response:**
```json
{
  "success": true,
  "sessionToken": "token-uuid",
  "message": "Login successful"
}
```

**Usage:**
```csharp
string response = mobileConnector.HandleLogin("device-123", "1.0.0");
// Returns JSON with sessionToken for subsequent requests
```

### Career Data

#### GET /api/career
Get player career statistics and progress.

**Headers:**
```
Authorization: Bearer {sessionToken}
```

**Response:**
```json
{
  "success": true,
  "level": 5,
  "totalExperience": 4250,
  "balance": 75500.0,
  "racesCompleted": 12,
  "wins": 3,
  "bestLapTime": 78.234
}
```

#### GET /api/career/races
Get race history with filtering.

**Query Parameters:**
- `count`: Number of recent races to retrieve (default: 10)

**Response:**
```json
{
  "success": true,
  "races": [
    {
      "eventName": "Monaco GP",
      "trackName": "Monaco",
      "position": 2,
      "bestLapTime": 78.234,
      "prizeMoney": 6000.0,
      "experiencePoints": 300,
      "completeDate": "2026-03-27T15:30:00"
    }
  ]
}
```

### Vehicle Upgrades

#### GET /api/upgrades/available
Get list of purchasable upgrades.

**Response:**
```json
{
  "success": true,
  "upgrades": [
    {
      "name": "Turbocharger",
      "type": "Engine",
      "cost": 15000.0,
      "performanceGain": 0.15,
      "requiredLevel": 5
    },
    {
      "name": "Carbon Suspension",
      "type": "Suspension",
      "cost": 12000.0,
      "performanceGain": 0.12,
      "requiredLevel": 4
    }
  ]
}
```

#### POST /api/upgrades/purchase
Purchase a vehicle upgrade.

**Request:**
```json
{
  "upgradeName": "Turbocharger"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Upgrade Turbocharger purchased"
}
```

### Vehicle Setups

#### GET /api/setups
Get all saved vehicle setups.

**Response:**
```json
{
  "success": true,
  "setups": [
    {
      "name": "Monaco Setup",
      "track": "Monaco",
      "bestLapTime": 78.234,
      "useCount": 5,
      "created": "2026-03-20T10:00:00"
    },
    {
      "name": "Monza Setup",
      "track": "Monza",
      "bestLapTime": 95.150,
      "useCount": 3,
      "created": "2026-03-25T14:30:00"
    }
  ]
}
```

#### POST /api/setups/load
Load a vehicle setup into the game.

**Request:**
```json
{
  "setupName": "Monaco Setup"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Setup Monaco Setup loaded"
}
```

## API Implementation

### Initialize Server

```csharp
var mobileConnector = gameObject.AddComponent<MobileAppConnector>();
mobileConnector.Initialize();
mobileConnector.StartServer();
```

### Handle Login Request

```csharp
string deviceId = "device-123";
string appVersion = "1.0.0";
string loginResponse = mobileConnector.HandleLogin(deviceId, appVersion);
// Parse JSON to extract sessionToken
```

### Get Career Data

```csharp
string sessionToken = "received-from-login";
string careerData = mobileConnector.GetCareerData(sessionToken);
// Parse JSON response with level, balance, races, wins
```

### Purchase Upgrade

```csharp
string response = mobileConnector.PurchaseUpgrade(sessionToken, "Turbocharger");
// Returns success/failure JSON
```

## Security Considerations

### Session Management
- **Token Generation**: UUID-based random tokens
- **Timeout**: 30-minute inactivity timeout
- **Validation**: Every request validates token and updates last activity

### Device Tracking
- Device IDs tracked for login audit
- Multiple sessions per device allowed
- Session termination on explicit logout

### Data Protection
- No sensitive data in URL parameters
- POST requests for write operations
- Session tokens in Authorization header

## Mobile App Integration

### Example iOS Swift Integration

```swift
import Foundation

class SendItAPI {
    let baseURL = "http://localhost:8080"
    var sessionToken: String?

    func login(deviceId: String) async throws {
        let loginData = ["deviceId": deviceId, "appVersion": "1.0.0"]
        let response = try await post("/api/login", data: loginData)
        sessionToken = response["sessionToken"] as? String
    }

    func getCareerData() async throws -> [String: Any] {
        return try await get("/api/career")
    }

    func purchaseUpgrade(_ name: String) async throws {
        try await post("/api/upgrades/purchase",
                      data: ["upgradeName": name])
    }

    private func get(_ endpoint: String) async throws -> [String: Any] {
        // Implementation with Authorization header
        // Headers: ["Authorization": "Bearer \(sessionToken ?? "")"]
        return [:]
    }

    private func post(_ endpoint: String,
                     data: [String: Any]) async throws -> [String: Any] {
        // Implementation with JSON body
        return [:]
    }
}
```

### Example Android Kotlin Integration

```kotlin
class SendItAPIClient {
    private val baseUrl = "http://localhost:8080"
    private val httpClient = OkHttpClient()
    private var sessionToken: String? = null

    suspend fun login(deviceId: String) {
        val body = mapOf(
            "deviceId" to deviceId,
            "appVersion" to "1.0.0"
        ).toJson()

        val response = post("/api/login", body)
        sessionToken = response["sessionToken"]?.asString
    }

    suspend fun getCareerData(): Map<String, Any> {
        return get("/api/career")
    }

    suspend fun purchaseUpgrade(name: String) {
        val body = mapOf("upgradeName" to name).toJson()
        post("/api/upgrades/purchase", body)
    }

    private suspend fun get(endpoint: String): Map<String, Any> {
        val request = Request.Builder()
            .url(baseUrl + endpoint)
            .header("Authorization", "Bearer $sessionToken")
            .build()
        // Execute and parse JSON
        return mapOf()
    }

    private suspend fun post(endpoint: String, body: String): Map<String, Any> {
        val requestBody = body.toRequestBody("application/json".toMediaType())
        val request = Request.Builder()
            .url(baseUrl + endpoint)
            .header("Authorization", "Bearer $sessionToken")
            .post(requestBody)
            .build()
        // Execute and parse JSON
        return mapOf()
    }
}
```

## Error Handling

### Common Error Responses

**Invalid Session:**
```json
{
  "success": false,
  "error": "Invalid session"
}
```

**Insufficient Funds:**
```json
{
  "success": false,
  "error": "Insufficient funds. Need $15000, have $12000"
}
```

**Level Requirement Not Met:**
```json
{
  "success": false,
  "error": "Career level 5 required. Upgrade needs level 5"
}
```

## API Status Monitoring

### Check Server Status

```csharp
string status = mobileConnector.GetApiStatus();
// Returns formatted status string with:
// - Server active state
// - Port number
// - API version
// - Active sessions count
// - Connected devices count
```

### Monitor Connections

```csharp
int activeSessions = mobileConnector.GetActiveSessionCount();
int connectedDevices = mobileConnector.GetConnectedDeviceCount();
```

## Performance Optimization

### Caching Strategy
- Career data cached for 1 minute
- Setup lists cached for 2 minutes
- Upgrade availability cached for 5 minutes

### Bandwidth Optimization
- JSON compression enabled
- Only changed fields in updates
- Pagination for large result sets

## Rate Limiting

- **Per Session**: 100 requests per minute
- **Per Device**: 500 requests per minute
- **Global**: 10,000 requests per minute

## API Versioning

**Current Version**: 1.0

**Compatibility Matrix:**
| Client | Game | Status |
|--------|------|--------|
| 1.0 | 1.0+ | ✓ Compatible |
| 2.0 | 2.0+ | Future |

## Future Enhancements

- [ ] WebSocket support for real-time updates
- [ ] Setup sharing between players
- [ ] Live race event notifications
- [ ] Multiplayer matchmaking integration
- [ ] OAuth2 authentication
- [ ] Analytics and performance tracking
- [ ] Voice command integration
- [ ] AR vehicle preview

## Testing Endpoints

### Manual Testing with cURL

```bash
# Login
curl -X POST http://localhost:8080/api/login \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"test-device","appVersion":"1.0.0"}'

# Get career data
curl -H "Authorization: Bearer {token}" \
  http://localhost:8080/api/career

# Get available upgrades
curl -H "Authorization: Bearer {token}" \
  http://localhost:8080/api/upgrades/available

# Purchase upgrade
curl -X POST http://localhost:8080/api/upgrades/purchase \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"upgradeName":"Turbocharger"}'
```

---

**Status**: ✓ Complete and integrated
**Last Updated**: 2026-03-27
**API Version**: 1.0
