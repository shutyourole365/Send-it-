/**
 * SendIt Mobile App - Shared API Client
 * Works with both iOS (Swift) and Android (Kotlin) via RN Bridge
 */

export interface CareerData {
  level: number;
  totalExperience: number;
  balance: number;
  racesCompleted: number;
  wins: number;
  bestLapTime: number;
}

export interface RaceResult {
  eventName: string;
  trackName: string;
  position: number;
  bestLapTime: number;
  prizeMoney: number;
  experiencePoints: number;
  completeDate: string;
}

export interface VehicleUpgrade {
  name: string;
  type: string;
  cost: number;
  performanceGain: number;
  requiredLevel: number;
}

export interface VehicleSetup {
  name: string;
  track: string;
  bestLapTime: number;
  useCount: number;
  created: string;
}

export class SendItAPI {
  private baseURL: string;
  private sessionToken: string | null = null;
  private deviceId: string;

  constructor(baseURL: string, deviceId: string) {
    this.baseURL = baseURL;
    this.deviceId = deviceId;
  }

  /**
   * Authenticate and create session
   */
  async login(): Promise<string> {
    try {
      const response = await fetch(`${this.baseURL}/api/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          deviceId: this.deviceId,
          appVersion: '1.0.0'
        })
      });

      if (!response.ok) {
        throw new Error(`Login failed: ${response.statusText}`);
      }

      const data = await response.json();
      this.sessionToken = data.sessionToken;
      return data.sessionToken;
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  }

  /**
   * Get career data
   */
  async getCareerData(): Promise<CareerData> {
    return this.makeAuthenticatedRequest('/api/career', 'GET');
  }

  /**
   * Get race history
   */
  async getRaceHistory(count: number = 10): Promise<RaceResult[]> {
    const response = await this.makeAuthenticatedRequest(
      `/api/career/races?count=${count}`,
      'GET'
    );
    return response.races || [];
  }

  /**
   * Get available upgrades
   */
  async getAvailableUpgrades(): Promise<VehicleUpgrade[]> {
    const response = await this.makeAuthenticatedRequest(
      '/api/upgrades/available',
      'GET'
    );
    return response.upgrades || [];
  }

  /**
   * Purchase upgrade
   */
  async purchaseUpgrade(upgradeName: string): Promise<boolean> {
    try {
      const response = await this.makeAuthenticatedRequest(
        '/api/upgrades/purchase',
        'POST',
        { upgradeName }
      );
      return response.success;
    } catch (error) {
      console.error('Purchase error:', error);
      return false;
    }
  }

  /**
   * Get saved vehicle setups
   */
  async getSavedSetups(): Promise<VehicleSetup[]> {
    const response = await this.makeAuthenticatedRequest(
      '/api/setups',
      'GET'
    );
    return response.setups || [];
  }

  /**
   * Load a vehicle setup
   */
  async loadSetup(setupName: string): Promise<boolean> {
    try {
      const response = await this.makeAuthenticatedRequest(
        '/api/setups/load',
        'POST',
        { setupName }
      );
      return response.success;
    } catch (error) {
      console.error('Load setup error:', error);
      return false;
    }
  }

  /**
   * Make authenticated API request
   */
  private async makeAuthenticatedRequest(
    endpoint: string,
    method: string,
    body?: any
  ): Promise<any> {
    if (!this.sessionToken) {
      throw new Error('Not authenticated. Call login() first.');
    }

    const options: RequestInit = {
      method,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.sessionToken}`
      }
    };

    if (body) {
      options.body = JSON.stringify(body);
    }

    const response = await fetch(`${this.baseURL}${endpoint}`, options);

    if (!response.ok) {
      if (response.status === 401) {
        // Session expired
        this.sessionToken = null;
        throw new Error('Session expired. Please login again.');
      }
      throw new Error(`API error: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Check if authenticated
   */
  isAuthenticated(): boolean {
    return this.sessionToken !== null;
  }

  /**
   * Logout and clear session
   */
  logout(): void {
    this.sessionToken = null;
  }

  /**
   * Set custom session token (for testing)
   */
  setSessionToken(token: string): void {
    this.sessionToken = token;
  }
}

/**
 * Shared UI components and utilities
 */
export class SendItUIUtils {
  /**
   * Format currency
   */
  static formatMoney(amount: number): string {
    return `$${amount.toFixed(0).replace(/\B(?=(\d{3})+(?!\d))/g, ',')}`;
  }

  /**
   * Format time
   */
  static formatTime(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = (seconds % 60).toFixed(2);
    return `${mins}:${secs}`;
  }

  /**
   * Format lap time
   */
  static formatLapTime(seconds: number): string {
    return seconds.toFixed(3);
  }

  /**
   * Get experience color
   */
  static getExperienceColor(progress: number): string {
    if (progress < 0.25) return '#FF6B6B';
    if (progress < 0.5) return '#FFA500';
    if (progress < 0.75) return '#4CAF50';
    return '#2196F3';
  }

  /**
   * Get damage color
   */
  static getDamageColor(damageLevel: number): string {
    if (damageLevel < 0.33) return '#4CAF50'; // Green
    if (damageLevel < 0.66) return '#FFA500'; // Orange
    return '#FF6B6B'; // Red
  }

  /**
   * Get position badge color
   */
  static getPositionColor(position: number): string {
    if (position === 1) return '#FFD700'; // Gold
    if (position === 2) return '#C0C0C0'; // Silver
    if (position === 3) return '#CD7F32'; // Bronze
    return '#9E9E9E'; // Gray
  }
}

/**
 * Local storage utilities
 */
export class StorageUtil {
  private prefix = '@SendIt_';

  /**
   * Save value to storage
   */
  async save(key: string, value: any): Promise<void> {
    try {
      const jsonValue = JSON.stringify(value);
      localStorage.setItem(this.prefix + key, jsonValue);
    } catch (error) {
      console.error('Storage save error:', error);
    }
  }

  /**
   * Load value from storage
   */
  async load<T = any>(key: string): Promise<T | null> {
    try {
      const jsonValue = localStorage.getItem(this.prefix + key);
      return jsonValue ? JSON.parse(jsonValue) : null;
    } catch (error) {
      console.error('Storage load error:', error);
      return null;
    }
  }

  /**
   * Remove value from storage
   */
  async remove(key: string): Promise<void> {
    try {
      localStorage.removeItem(this.prefix + key);
    } catch (error) {
      console.error('Storage remove error:', error);
    }
  }

  /**
   * Clear all storage
   */
  async clear(): Promise<void> {
    try {
      const keys = Object.keys(localStorage);
      keys.forEach(key => {
        if (key.startsWith(this.prefix)) {
          localStorage.removeItem(key);
        }
      });
    } catch (error) {
      console.error('Storage clear error:', error);
    }
  }
}
