## Project Overview
This is a Unity-based VR application for the study "Cognitive and Behavioral Effects of Gamification Mechanics in Immersive Data Visualization Environments in Virtual Reality (VR)". The system presents visual stimuli while tracking user response time, task accuracy, gaze patterns, pupil data, eye geometry, and head movements.

## Technical Requirements

- **Unity Version**: 2022.3.21f1
- **XR Interaction Toolkit**: 3.0.8
- **OpenXR**: 1.10.0
- **Hardware**: HTC Vive Pro with eye tracking module
- **Platform**: PC VR (Windows)

## Core Features
### 1. Behavioral Tracking System
- **HTC Vive Eye Integration**: Full support for binocular eye tracking
- **Gaze Data**: Real-time capture of gaze position, rotation, and validity
- **Drift Calibration**: Automatic compensation for gaze drift over time
- **Pupil Metrics**: Diameter and position tracking for both eyes
- **Eye Geometry**: Openness, squeeze, and wide metrics
- **Head Pose**: Global coordinate and forward vector computed in real-time
- **Per-Image Tracking**: Organized data collection indexed by presented stimuli

### 2. Study Flow Management
- **Question Display System**: Dynamic presentation of study questions with multiple response formats
- **Task Management**: Support for different task types and families
- **Response Validation**: Built-in answer checking and feedback mechanisms

### 3. VR Interaction
- **XR Interaction Toolkit 3.0**: Modern Unity XR framework implementation
- **Controller Support**: Full HTC Vive controller integration with trigger and trackpad inputs
- **UI Interaction**: VR-optimized UI elements for simultaneous in-headset interaction and external inspection

## Setup Instructions

### 1. Unity Project Configuration

1. Clone this project and open it in **Unity 2022.3.21f1**
2. Make sure XR Plugin Management is enabled in Project Settings
3. Configure OpenXR as the XR provider for PC Standalone

### 2. VIVE OpenXR Setup

The project includes a VIVE OpenXR installer utility accessible via:
- **Menu**: `VIVE > OpenXR Installer > Install or Update latest version`

### 3. XR Settings Configuration

Key OpenXR features to enable:
- **Eye Gaze Interaction Profile** (Required for eye tracking)
- **VIVE XR Eye Tracker** (Beta feature)

### 4. Scene Selection

**Full Study Flow**:
1. Do a five-point eye calibration in SteamVR
2. Open `Assets/Scenes/Setup`
3. Enter play mode → the user will see a red sphere in the center
4. Experimenter: pick one of the six sequences from dropdown
5. Hit **validate** (for drift calibration) → check console (target values for raw drift (<2°) and residual error (<0.5°))
6. Hit **start** to begin the study
7. Logs auto-save every block (in the editor menu: `Tools > Open Study Log Folder`)
8. In Break scene: run **validate** again before next block
9. Exit play mode once study is done (console will confirm)

**Quick Demo (slice of study)**:
1. Open `Assets/Scenes/Study` or `Assets/Scenes/Test`
2. Enter play mode

## Input Mappings

### Left/Right Controller
- **Trigger**: Primary interaction button
- **Trackpad Click**: Secondary action
- **Trackpad Position**: 2D axis input
- **Position/Rotation**: 6DOF tracking

## Troubleshooting

### Eye Tracking Not Working
1. Make sure **VIVE SRAnipal Runtime** is running (restart if stuck)
2. Check **OpenXR** → **Eye Gaze Interaction Profile** is enabled
3. Confirm VIVE XR Eye Tracker is active
4. Redo eye calibration in SteamVR

### Input Not Responding
1. Verify controller bindings in **Study Input Actions**
2. Check **XR Interaction Manager** is present in scene
3. Ensure interaction layers are properly configured
4. Validate OpenXR runtime is active

## Support

For questions about the HTC Vive eye tracking integration, refer to:
- [HTC VIVE OpenXR Documentation](https://github.com/ViveSoftware/VIVE-OpenXR)
- [Unity XR Interaction Toolkit Documentation](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0)
- [OpenXR Specification](https://www.khronos.org/openxr/)
