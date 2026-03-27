# SendIt Mobile App Development Guide

## Overview

The SendIt Mobile App provides remote access to game career data, upgrade management, and setup sharing. Available for both iOS (SwiftUI) and Android (Jetpack Compose).

## App Architecture

### Shared Components

**SendItAPI (TypeScript/Shared)**
- Cross-platform API client
- Data models and structures
- UI utilities (formatting, colors)
- Storage utilities

### iOS Implementation (SwiftUI)

**SendItAPI.swift**
- Async/await API client
- ObservableObject for reactive updates
- URLSession networking
- UserDefaults session storage

**CareerView.swift**
- Career progress display
- Level and experience visualization
- Race history
- Upgrade marketplace

### Android Implementation (Jetpack Compose)

**SendItAPI.kt**
- Coroutine-based API client
- OkHttpClient networking
- SharedPreferences session storage
- Serialization with Kotlinx

**CareerScreen.kt**
- Material 3 UI components
- Real-time data updates
- Composable screens

## Setup Instructions

### iOS Setup

#### Prerequisites
- Xcode 14+
- iOS 14+
- Swift 5.7+

#### Installation

1. **Create Xcode Project**
```bash
mkdir SendItApp
cd SendItApp
swift package init --type app
```

2. **Add Dependencies** (Package.swift)
```swift
dependencies: [
    .package(url: "https://github.com/alamofire/alamofire.git", from: "5.0.0")
]
```

3. **Add Files**
   - Copy `SendItAPI.swift` to project
   - Copy `Views/CareerView.swift` to project
   - Create necessary UI views

4. **Configure Base URL**
```swift
let apiClient = SendItAPIClient(baseURL: "http://your-game-server:8080")
```

5. **Run App**
```bash
xcodebuild -scheme SendItApp -configuration Debug
```

### Android Setup

#### Prerequisites
- Android Studio Arctic Fox+
- Android 7.0+ (API 24)
- Kotlin 1.7+
- Gradle 7.0+

#### Installation

1. **Create Project**
```bash
android-studio --new-project SendItApp
```

2. **Add Dependencies** (build.gradle.kts)
```kotlin
dependencies {
    // Networking
    implementation("com.squareup.okhttp3:okhttp:4.10.0")

    // Serialization
    implementation("org.jetbrains.kotlinx:kotlinx-serialization-json:1.4.1")

    // Compose
    implementation("androidx.compose.ui:ui:1.3.3")
    implementation("androidx.compose.material3:material3:1.0.0")

    // Coroutines
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.6.4")
}
```

3. **Add Files**
   - Copy `SendItAPI.kt` to `api/` package
   - Copy `CareerScreen.kt` to `ui/` package
   - Create Theme and Navigation

4. **Configure Base URL**
```kotlin
val apiClient = SendItAPIClient(
    context = applicationContext,
    baseURL = "http://your-game-server:8080"
)
```

5. **Run App**
```bash
./gradlew assembleDebug
```

## API Integration

### Authentication Flow

```typescript
// 1. Initialize API client
const api = new SendItAPI("http://localhost:8080", deviceId);

// 2. Login to create session
const sessionToken = await api.login();
// Returns token, stored in localStorage/UserDefaults

// 3. Make authenticated requests
const careerData = await api.getCareerData();
// All subsequent requests use stored token
```

### Career Data Access

```swift
// iOS Example
@StateObject private var apiClient = SendItAPIClient()

func fetchCareer() async {
    do {
        try await apiClient.fetchCareerData()
        // apiClient.careerData is updated
    } catch {
        print("Error: \(error.localizedDescription)")
    }
}
```

```kotlin
// Android Example
val apiClient = SendItAPIClient(context)

lifecycleScope.launch {
    apiClient.fetchCareerData()
        .onSuccess { careerData ->
            // Update UI with careerData
        }
        .onFailure { error ->
            // Handle error
        }
}
```

### Race History

```swift
let races = try await apiClient.fetchRaceHistory(count: 10)
// Returns sorted list of RaceResult objects
```

### Upgrades

```kotlin
val upgrades = apiClient.fetchAvailableUpgrades()
    .getOrNull() ?: emptyList()

// Purchase upgrade
apiClient.purchaseUpgrade("Turbocharger")
    .onSuccess { success ->
        if (success) showToast("Upgrade purchased!")
    }
```

### Setup Management

```swift
// Get setups
let setups = try await apiClient.getSavedSetups()

// Load setup
let success = try await apiClient.loadSetup(name: "Monaco Setup")
```

## UI Components

### iOS Components

**CareerView**
- Master view containing all career data
- Requires authentication
- Displays level, stats, races, upgrades

**LevelCard**
- Shows level and experience
- XP progress bar with color coding

**StatsGrid**
- Grid layout of 4 stats cards
- Races, Wins, Best Lap, Win Rate

**RaceResultRow**
- Individual race result item
- Position badge with color coding
- Prize money display

**UpgradeRow**
- Upgrade card with purchase button
- Performance gain percentage
- Cost display

### Android Components

**CareerScreen**
- Main composable screen
- Handles data loading and error states
- Lazy column for scroll performance

**LevelCard**
- Material 3 card with level info
- Linear progress indicator
- Balance display

**StatCard**
- Reusable stat display component
- Icon, value, and label
- Equal width layout

**RaceResultCard**
- Composable race item
- Assist chips for position and track
- Performance metrics

**UpgradeCard**
- Material 3 card with purchase button
- Loading state during purchase
- Performance gain highlight

## Styling & Theming

### iOS Theme

```swift
extension Color {
    static let careerGreen = Color(red: 0.76, green: 0.89, blue: 0.24)
    static let careerBlue = Color(red: 0.0, green: 0.48, blue: 1.0)
    static let careerRed = Color(red: 1.0, green: 0.42, blue: 0.42)
}
```

### Android Theme

```kotlin
// material3 colors
Material3Theme(
    colorScheme = lightColorScheme(
        primary = Color(0xFF2196F3),
        onPrimary = Color.White,
        secondary = Color(0xFF4CAF50),
        background = Color(0xFFFAFAFA)
    )
)
```

## Network Configuration

### Dev Environment

```swift
let apiClient = SendItAPIClient(baseURL: "http://localhost:8080")
```

### Production Environment

```swift
let apiClient = SendItAPIClient(baseURL: "https://api.sendit.example.com")
```

## Error Handling

### iOS

```swift
do {
    try await apiClient.login()
} catch APIError.sessionExpired {
    // Redirect to login
} catch APIError.networkError(let message) {
    print("Network error: \(message)")
} catch {
    print("Unknown error: \(error)")
}
```

### Android

```kotlin
apiClient.fetchCareerData()
    .onSuccess { data ->
        // Handle success
    }
    .onFailure { exception ->
        when (exception) {
            is IOException -> showNetworkError()
            else -> showGeneralError()
        }
    }
```

## Storage

### Session Persistence

**iOS (UserDefaults)**
```swift
UserDefaults.standard.set(token, forKey: "sessionToken")
let token = UserDefaults.standard.string(forKey: "sessionToken")
```

**Android (SharedPreferences)**
```kotlin
val prefs = context.getSharedPreferences("SendIt", Context.MODE_PRIVATE)
prefs.edit().putString("sessionToken", token).apply()
val token = prefs.getString("sessionToken", null)
```

## Testing

### Unit Tests

**iOS**
```swift
class SendItAPIClientTests: XCTestCase {
    func testLogin() async throws {
        let client = SendItAPIClient()
        let token = try await client.login()
        XCTAssertNotNil(token)
    }
}
```

**Android**
```kotlin
class SendItAPIClientTest {
    @Test
    fun testLogin() = runTest {
        val result = apiClient.login()
        assertTrue(result.isSuccess)
    }
}
```

### UI Tests

**iOS**
```swift
class CareerViewTests: XCTestCase {
    func testCareerDataDisplay() {
        let view = CareerView()
        // Assert career data is displayed
    }
}
```

**Android**
```kotlin
@RunWith(AndroidJUnit4::class)
class CareerScreenTest {
    @Test
    fun testCareerDataDisplay() {
        composeTestRule.setContent {
            CareerScreen(apiClient)
        }
        // Assert UI elements exist
    }
}
```

## Performance Optimization

### Image Caching

```swift
// iOS - Use URLCache
let config = URLSessionConfiguration.default
config.urlCache = URLCache.shared
let session = URLSession(configuration: config)
```

### List Optimization

```kotlin
// Android - Use rememberLazyListState
val listState = rememberLazyListState()
LazyColumn(state = listState) {
    // Only renders visible items
}
```

### API Call Debouncing

```swift
// Debounce refresh requests
@State private var debounceWorkItem: DispatchWorkItem?

func debouncedRefresh() {
    debounceWorkItem?.cancel()
    debounceWorkItem = DispatchWorkItem {
        Task { await fetchData() }
    }
    DispatchQueue.main.asyncAfter(
        deadline: .now() + 0.5,
        execute: debounceWorkItem!
    )
}
```

## Deployment

### iOS Release

1. **Build**
```bash
xcodebuild archive -scheme SendItApp -archivePath ./SendItApp.xcarchive
```

2. **Export**
```bash
xcodebuild -exportArchive -archivePath ./SendItApp.xcarchive \
  -exportOptionsPlist options.plist \
  -exportPath ./
```

3. **Upload to App Store**
```bash
xcrun altool --upload-app --file SendItApp.ipa \
  --type ios --username email@example.com
```

### Android Release

1. **Build Release APK**
```bash
./gradlew assembleRelease
```

2. **Sign APK**
```bash
jarsigner -verbose -sigalg SHA256withRSA \
  -digestalg SHA-256 \
  app-release.apk keystore.jks
```

3. **Upload to Play Store**
```bash
bundletool upload-bundle --bundle=SendItApp.aab \
  --ks=keystore.jks
```

## Troubleshooting

### Connection Issues

**Problem**: Cannot connect to game server
**Solution**:
- Verify game server is running: `curl http://localhost:8080/api/login`
- Check network connectivity
- Verify base URL in app configuration
- For iOS, allow cleartext traffic (Development only):
  ```xml
  <key>NSAppTransportSecurity</key>
  <dict>
    <key>NSAllowsLocalNetworking</key>
    <true/>
  </dict>
  ```

### Authentication Failures

**Problem**: Session expired error
**Solution**:
- Clear stored session token
- Re-login with fresh credentials
- Check server session timeout configuration

### Data Not Updating

**Problem**: Career data doesn't refresh
**Solution**:
- Verify network request is made
- Check API response in network inspector
- Ensure `fetchCareerData()` is called
- For Android, verify coroutine scope is active

## Best Practices

1. **Always handle authentication errors**
   - Check `isAuthenticated` before making requests
   - Redirect to login on 401 responses

2. **Provide loading states**
   - Show progress indicator during data loading
   - Disable buttons during async operations

3. **Cache appropriately**
   - Cache career data for offline access
   - Refresh on app resume
   - TTL-based cache invalidation

4. **Error messages**
   - Show user-friendly error messages
   - Log detailed errors for debugging
   - Provide retry options

5. **Performance**
   - Load only necessary data initially
   - Implement pagination for large lists
   - Debounce rapid requests

## API Reference Quick

```
POST /api/login
GET  /api/career
GET  /api/career/races?count=10
GET  /api/upgrades/available
POST /api/upgrades/purchase
GET  /api/setups
POST /api/setups/load
```

See Mobile API Documentation for full details.

---

**Status**: ✓ iOS & Android Implementations Complete
**Last Updated**: 2026-03-27
**Platforms**: iOS 14+ | Android 7.0+
