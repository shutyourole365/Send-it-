# Send-it-
A realistic Burnout Simulator with deep graphics and physics customization. Build, tune, and drive your dream vehicle with professional-grade physics simulation.

## Features

### Physics Simulation System
- **Realistic Engine Dynamics**: Torque curves based on RPM with configurable horsepower and responsiveness
- **Advanced Transmission**: Customizable gear ratios with realistic power delivery
- **Suspension Physics**: Per-wheel spring/damper system with adjustable stiffness, damping, and ride height
- **Tire Dynamics**: Complex grip simulation with slip angles, temperature effects, and wear simulation
- **Aerodynamics**: Drag and downforce calculations for high-speed stability and control

### Comprehensive Tuning System
Modify every aspect of your vehicle's behavior:
- **Engine**: Max RPM, horsepower, torque curve, responsiveness
- **Suspension**: Spring stiffness, damping ratios, ride height, anti-roll bars (per corner)
- **Tires**: Grip coefficient, slip angles, temperature sensitivity, wear rates
- **Aerodynamics**: Drag coefficient, downforce, spoiler angle
- **Weight**: Total mass and weight distribution

### Graphics Customization
- **Paint System**: Custom colors, metallic intensity, gloss, pearlescent effects
- **Body Modifications**: Wheel sizes/offsets, spoiler angles, body kits, bumper styles
- **Material Customization**: Shader parameters, weathering, wear simulation, rust effects
- **Real-time Rendering**: Shadows, reflections, ambient occlusion, motion blur effects

## Project Structure

```
Assets/
├── Scripts/
│   ├── Physics/          # Engine, suspension, tire, aero simulation
│   ├── Tuning/          # Parameter management and tuning system
│   ├── Graphics/        # Visual customization systems
│   ├── UI/              # Tuning interface and controls
│   ├── Data/            # Vehicle configuration and save/load
│   └── Gameplay/        # Game orchestration
├── Prefabs/             # Vehicle and component prefabs
├── Materials/           # Custom shaders and materials
├── Scenes/              # Game scenes (Main, Garage, Track Selection)
└── Resources/           # Presets and configuration files
```

## Getting Started

### Prerequisites
- Unity 2021.3 LTS or later
- Havok Physics or built-in Physics engine

### Installation
1. Clone the repository
2. Open the project in Unity
3. Load the main scene from Assets/Scenes/
4. Customize your vehicle in the tuning garage

## System Architecture

### Core Systems

**TuningManager**: Central hub for all parameter management
- Stores and manages physics and graphics parameters
- Provides real-time callbacks when values change
- Handles parameter validation and constraints

**VehicleController**: Main physics integration
- Applies forces to vehicle rigidbody
- Integrates all physics subsystems
- Responds to tuning changes in real-time

**Physics Engine**: Realistic vehicle dynamics
- Engine: Torque curve simulation
- Transmission: Gear ratio calculations
- Suspension: Spring/damper forces
- Tire: Grip and slip modeling
- Aerodynamics: Drag and downforce

**VehicleData**: Serializable configuration model
- Complete vehicle setup in JSON format
- Save/load custom vehicle configurations
- Physics and graphics parameters

## Development Roadmap

- [x] Phase 1: Core physics architecture and tuning system
- [ ] Phase 2: Advanced physics implementation (slip, temperature, wear)
- [ ] Phase 3: Physics tuning UI with live preview
- [ ] Phase 4: Graphics customization system
- [ ] Phase 5: Advanced rendering and effects
- [ ] Phase 6: Polish and optimization

## License
MIT
