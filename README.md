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
- [x] Phase 2: Advanced physics implementation (slip, temperature, wear, pressure, surfaces)
- [ ] Phase 3: Physics tuning UI with live preview
- [ ] Phase 4: Graphics customization system
- [x] Phase 5: Advanced rendering and effects
- [ ] Phase 6: Polish and optimization

## Phase 2: Advanced Physics Systems

Phase 2 introduces sophisticated tire physics with 5 integrated subsystems:

### Tire Slip Dynamics
- Load-dependent peak slip angles and ratios
- Load transfer effects (lateral and longitudinal)
- Slip envelope calculation combining lateral and longitudinal grip
- Realistic grip curves with smooth damping

### Tire Temperature System
- Localized temperature zones (center, edges, walls)
- Heat generation from friction and slip
- Temperature-dependent grip curves
- Temperature effect on wear acceleration

### Tire Wear Patterns
- Location-specific wear tracking (center, edges, walls)
- Tread depth simulation (8mm → 1.6mm minimum)
- Multiple tire compounds with different wear rates
- Wear pattern detection and uneven wear penalties

### Tire Pressure System
- Temperature-pressure relationship (ideal gas law)
- Pressure effects on grip and wear
- Under/over-pressure penalties
- Blowout risk detection

### Surface Conditions System
- 10 road surface types with unique properties
- Wetness levels and dynamic grip changes
- Aquaplaning simulation
- Temperature effects on grip

See `Assets/Scripts/Physics/PHASE2_DOCUMENTATION.md` for detailed system documentation.

## Phase 5: Advanced Rendering & Effects

Phase 5 introduces cinematic visual effects with 5 integrated rendering systems:

### Motion Blur Effect
- Velocity-based motion blur using frame accumulation
- Adjustable sample count (4-16 samples)
- Shutter angle control for artistic effects
- Temporal filtering for smooth transitions

### Depth of Field Effect
- Auto-focus tracking with smooth transitions
- Multiple bokeh shapes (Circle, Hexagon, Octagon, Diamond)
- Circle of Confusion (CoC) calculation
- f-stop aperture simulation (1.4-32.0)

### Particle Effect System
- Dynamic tire smoke from slip and temperature
- Dust generation on loose surfaces
- Water spray on wet roads
- Spark generation from impacts
- Engine overheat smoke
- Brake glow effects

### Dynamic Lighting System
- Headlight and brake light management
- 24-hour time-of-day lighting cycles
- Engine glow based on temperature
- Smooth light response curves
- Ambient color shifts throughout day

### Advanced Shadow System
- Cascaded shadows (1-4 cascades)
- Contact shadow detection
- Dynamic shadow distance optimization
- Multiple quality levels (Low/Medium/High/Ultra)
- Reflection probe management

See `Assets/Scripts/Graphics/PHASE5_DOCUMENTATION.md` for detailed system documentation.

## License
MIT
