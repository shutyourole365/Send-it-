import SwiftUI

struct CareerView: View {
    @StateObject private var apiClient = SendItAPIClient()
    @State private var showingAlert = false
    @State private var alertMessage = ""

    var body: some View {
        NavigationView {
            VStack(spacing: 0) {
                if apiClient.isAuthenticated {
                    ScrollView {
                        VStack(spacing: 20) {
                            // Level Card
                            if let career = apiClient.careerData {
                                LevelCard(careerData: career)

                                // Stats Grid
                                StatsGrid(careerData: career)

                                // Recent Races
                                RaceHistorySection()

                                // Upgrades
                                UpgradesSection()
                            } else {
                                ProgressView("Loading career data...")
                                    .onAppear {
                                        Task {
                                            do {
                                                try await apiClient.fetchCareerData()
                                            } catch {
                                                alertMessage = "Failed to load career data: \(error.localizedDescription)"
                                                showingAlert = true
                                            }
                                        }
                                    }
                            }
                        }
                        .padding()
                    }
                } else {
                    LoginView(apiClient: apiClient)
                }
            }
            .navigationTitle("Career")
            .alert("Error", isPresented: $showingAlert) {
                Button("OK", role: .cancel) { }
            } message: {
                Text(alertMessage)
            }
        }
    }
}

struct LevelCard: View {
    let careerData: CareerData

    var body: some View {
        VStack(spacing: 12) {
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Level \(careerData.level)")
                        .font(.title2)
                        .fontWeight(.bold)

                    Text("\(careerData.totalExperience) XP")
                        .font(.caption)
                        .foregroundColor(.gray)
                }

                Spacer()

                VStack(alignment: .trailing, spacing: 4) {
                    Text(careerData.balance.formatMoney())
                        .font(.title3)
                        .fontWeight(.semibold)
                        .foregroundColor(.green)

                    Text("Balance")
                        .font(.caption)
                        .foregroundColor(.gray)
                }
            }

            // Experience bar
            ProgressView(
                value: Double(careerData.totalExperience % 1000),
                total: 1000
            )
            .tint(getExperienceColor(careerData.totalExperience))
        }
        .padding()
        .background(Color(.systemGray6))
        .cornerRadius(12)
    }

    private func getExperienceColor(_ xp: Int) -> Color {
        let progress = Double((xp % 1000)) / 1000.0
        if progress < 0.33 { return .red }
        if progress < 0.66 { return .orange }
        return .green
    }
}

struct StatsGrid: View {
    let careerData: CareerData

    var body: some View {
        VStack(spacing: 12) {
            HStack(spacing: 12) {
                StatCard(
                    title: "Races",
                    value: "\(careerData.racesCompleted)",
                    icon: "flag.checkered"
                )

                StatCard(
                    title: "Wins",
                    value: "\(careerData.wins)",
                    icon: "crown.fill"
                )
            }

            HStack(spacing: 12) {
                StatCard(
                    title: "Best Lap",
                    value: careerData.bestLapTime.formatLapTime() + "s",
                    icon: "timer"
                )

                StatCard(
                    title: "Winrate",
                    value: "\(Int(Double(careerData.wins) / Double(max(1, careerData.racesCompleted)) * 100))%",
                    icon: "chart.pie.fill"
                )
            }
        }
    }
}

struct StatCard: View {
    let title: String
    let value: String
    let icon: String

    var body: some View {
        VStack(spacing: 8) {
            Image(systemName: icon)
                .font(.title2)
                .foregroundColor(.blue)

            Text(value)
                .font(.headline)
                .fontWeight(.bold)

            Text(title)
                .font(.caption)
                .foregroundColor(.gray)
        }
        .frame(maxWidth: .infinity)
        .padding()
        .background(Color(.systemGray6))
        .cornerRadius(12)
    }
}

struct RaceHistorySection: View {
    @StateObject private var apiClient = SendItAPIClient()
    @State private var raceHistory: [RaceResult] = []
    @State private var isLoading = false

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Recent Races")
                .font(.headline)
                .fontWeight(.bold)

            if isLoading {
                ProgressView()
            } else if raceHistory.isEmpty {
                Text("No races yet")
                    .foregroundColor(.gray)
                    .frame(maxWidth: .infinity, alignment: .center)
                    .padding()
            } else {
                VStack(spacing: 8) {
                    ForEach(raceHistory.prefix(5), id: \.completeDate) { race in
                        RaceResultRow(race: race)
                    }
                }
            }
        }
        .onAppear {
            Task {
                isLoading = true
                do {
                    raceHistory = try await apiClient.fetchRaceHistory()
                } catch {
                    // Handle error
                }
                isLoading = false
            }
        }
    }
}

struct RaceResultRow: View {
    let race: RaceResult

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(race.eventName)
                    .font(.headline)
                    .fontWeight(.semibold)

                HStack(spacing: 8) {
                    Badge(text: "P\(race.position)", color: getPositionColor(race.position))
                    Badge(text: race.trackName, color: .blue)
                }
            }

            Spacer()

            VStack(alignment: .trailing, spacing: 4) {
                Text(race.bestLapTime.formatLapTime() + "s")
                    .font(.subheadline)
                    .fontWeight(.semibold)

                Text(race.prizeMoney.formatMoney())
                    .font(.caption)
                    .foregroundColor(.green)
            }
        }
        .padding()
        .background(Color(.systemGray6))
        .cornerRadius(8)
    }

    private func getPositionColor(_ position: Int) -> Color {
        switch position {
        case 1: return .yellow
        case 2: return .gray
        case 3: return .orange
        default: return .blue
        }
    }
}

struct Badge: View {
    let text: String
    let color: Color

    var body: some View {
        Text(text)
            .font(.caption)
            .fontWeight(.semibold)
            .foregroundColor(.white)
            .padding(.horizontal, 8)
            .padding(.vertical, 4)
            .background(color)
            .cornerRadius(6)
    }
}

struct UpgradesSection: View {
    @StateObject private var apiClient = SendItAPIClient()
    @State private var upgrades: [VehicleUpgrade] = []
    @State private var isLoading = false

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Available Upgrades")
                .font(.headline)
                .fontWeight(.bold)

            if isLoading {
                ProgressView()
            } else {
                VStack(spacing: 8) {
                    ForEach(upgrades.prefix(3), id: \.name) { upgrade in
                        UpgradeRow(upgrade: upgrade, onPurchase: {
                            Task {
                                do {
                                    _ = try await apiClient.purchaseUpgrade(name: upgrade.name)
                                } catch {
                                    // Handle error
                                }
                            }
                        })
                    }
                }
            }
        }
        .onAppear {
            Task {
                isLoading = true
                do {
                    upgrades = try await apiClient.fetchAvailableUpgrades()
                } catch {
                    // Handle error
                }
                isLoading = false
            }
        }
    }
}

struct UpgradeRow: View {
    let upgrade: VehicleUpgrade
    let onPurchase: () -> Void

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(upgrade.name)
                    .font(.headline)
                    .fontWeight(.semibold)

                Text("+\(Int(upgrade.performanceGain * 100))% Performance")
                    .font(.caption)
                    .foregroundColor(.green)
            }

            Spacer()

            VStack(alignment: .trailing, spacing: 4) {
                Text(upgrade.cost.formatMoney())
                    .font(.headline)
                    .fontWeight(.semibold)

                Button(action: onPurchase) {
                    Text("Buy")
                        .font(.caption)
                        .fontWeight(.semibold)
                        .foregroundColor(.white)
                        .padding(.horizontal, 12)
                        .padding(.vertical, 4)
                        .background(Color.blue)
                        .cornerRadius(6)
                }
            }
        }
        .padding()
        .background(Color(.systemGray6))
        .cornerRadius(8)
    }
}

struct LoginView: View {
    @ObservedObject var apiClient: SendItAPIClient
    @State private var isLoading = false
    @State private var errorMessage = ""

    var body: some View {
        VStack(spacing: 20) {
            Image(systemName: "car.fill")
                .font(.system(size: 60))
                .foregroundColor(.blue)

            Text("SendIt")
                .font(.title)
                .fontWeight(.bold)

            Text("Career Manager")
                .foregroundColor(.gray)

            Spacer()

            Button(action: {
                Task {
                    isLoading = true
                    do {
                        try await apiClient.login()
                    } catch {
                        errorMessage = error.localizedDescription
                    }
                    isLoading = false
                }
            }) {
                if isLoading {
                    ProgressView()
                        .tint(.white)
                } else {
                    Text("Connect to Game")
                        .font(.headline)
                        .fontWeight(.semibold)
                        .foregroundColor(.white)
                }
            }
            .frame(maxWidth: .infinity)
            .padding()
            .background(Color.blue)
            .cornerRadius(12)
            .disabled(isLoading)

            if !errorMessage.isEmpty {
                Text(errorMessage)
                    .font(.caption)
                    .foregroundColor(.red)
                    .multilineTextAlignment(.center)
            }
        }
        .padding()
    }
}

#Preview {
    CareerView()
}
