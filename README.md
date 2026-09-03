# 🚁 Flood ResQ – AI-Powered Drone Rescue Simulator

> An AI-powered multi-drone simulation system designed to assist
> flood rescue operations through autonomous scanning, victim
> detection, location tracking, and emergency supply delivery.

## 🏆 Smart India Hackathon 2026

**Flood ResQ** is a simulation-based disaster response system
developed as our project for **Smart India Hackathon 2026**.

We have successfully **qualified Round 1 of SIH 2026** and are
continuing development of the prototype.

## 🎯 Problem Statement

Flood disasters create several challenges for rescue teams:

- Difficult access to flooded regions
- Delayed identification of stranded victims
- Risk to human rescuers
- Limited visibility over large affected areas
- Difficulty delivering emergency supplies quickly

Our project explores how an intelligent drone swarm can assist
rescue teams by providing rapid aerial scanning, AI-based victim
detection, mapping, and emergency supply delivery.

## 💡 Our Solution

Flood ResQ creates a virtual flooded environment in which multiple
drones work together to perform rescue-related operations.

The system combines:

- 🚁 Multi-drone swarm simulation
- 🤖 YOLO-based victim detection
- 🎙️ Voice-controlled operations
- 📍 Victim tracking
- 🗺️ Rescue mapping
- 📊 Monitoring dashboard
- 📦 Emergency supply delivery simulation

The operator can issue commands through **voice**, and the Unity
simulation responds by performing the corresponding operation.

Example:

>** "Start autonomous scan" **

The drone swarm begins its scanning operation and the AI detection
pipeline processes the environment for potential victims.

## 🏗️ System Architecture

                 🎙️ Voice Command
                        │
                        ▼
              ┌───────────────────┐
              │ Unity Voice       │
              │ Control System    │
              └─────────┬─────────┘
                        │
                        ▼
              ┌───────────────────┐
              │ Drone Swarm       │
              │ Controller        │
              └─────────┬─────────┘
                        │
                ┌───────┴───────┐
                ▼               ▼
          🚁 Drone System    🗺️ Rescue Map
                │
                ▼
          📷 Camera Frames
                │
                ▼
        ┌───────────────────┐
        │ Python YOLO       │
        │ Detection Pipeline│
        └─────────┬─────────┘
                  │
                  ▼
          🤖 Victim Detection
                  │
                  ▼
          📍 Victim Tracking
                  │
                  ▼
          📊 Rescue Dashboard
                  │
                  ▼
          📦 Supply Delivery


## 🤖 AI-Based Victim Detection

The project uses a YOLO-based computer vision pipeline for
detecting victims in the simulated flood environment.

The detection pipeline works conceptually as follows:

Unity Simulation
       ↓
Camera / Image Frame
       ↓
Python Detection Pipeline
       ↓
YOLO Model
       ↓
Victim Detection
       ↓
Unity
       ↓
Victim Marker & Tracking

The system also includes victim tracking logic.

When a victim is detected:

If it is a new victim, a new record/marker is created.
If the victim has already been detected, the existing record is
updated instead of creating a duplicate.
🎙️ Voice-Controlled Rescue Operations

Voice is the primary human-control mechanism of the simulator.

The operator can issue commands without manually controlling each
drone.

Example commands include:

 Start autonomous scan
 Stop

The voice command is interpreted by the Unity control system and
the corresponding operation is performed by the drone swarm.

## 🚁 Multi-Drone Rescue System

The simulator contains multiple drones operating inside the
virtual flood environment.

The drones can participate in operations such as:

- Autonomous scanning
- Victim detection
- Victim location tracking
- Rescue mapping
- Emergency supply delivery

The system is designed around coordinated drone operations rather
than requiring the operator to manually fly every drone.

## 🗺️ Rescue Monitoring Dashboard

The simulator includes a monitoring interface for observing the
rescue mission.

The dashboard can provide information related to:

- Drone status
- Drone positions
- Victim detections
- Victim locations
- Mission status
- Rescue operations

This provides the operator with a centralized view of the
simulation.

## 📦 Emergency Supply Delivery

The project also demonstrates the concept of using drones to
deliver emergency medical supplies to affected areas.

A possible operational flow is:

Detect affected area
        ↓
Identify victim / target
        ↓
Determine target location
        ↓
Deploy supply drone
        ↓
Deliver emergency supplies


# 🛠️ Technologies Used

## Simulation
- Unity 6.3 LTS
- Unity URP

- C#
- Artificial Intelligence
- YOLO
- Python
- Computer Vision
- Object Detection
- Communication
- Unity ↔ Python local socket communication
- Development Tools
- Visual Studio / VS Code
- Git
- GitHub

## 📂 Project Structure
Flood ResQ Drone Simulatorr/
│
├── Assets/
│   ├── Scenes/
│   ├── Scripts/
│   ├── Prefabs/
│   ├── Resources/
│   ├── PolyOne/
│   ├── TextMesh Pro/
│   └── ...
│
├── Packages/
│
├── ProjectSettings/
│
├── TrainingData/
│   ├── image_0000.png
│   ├── image_0000.txt
│   ├── image_0001.png
│   ├── image_0001.txt
│   └── ...
│
├── .gitignore
└── README.md

# 👥 Team

This project is being developed collaboratively as a team for
**Smart India Hackathon 2026**.

##  Team Member 	       Contribution
  - Manish Thakur	        Voice Control
  - Aryan Sharma	        Unity / Simulation
  - Anurag Yadav	        Drone System
  - Aparna Mishra	        AI / YOLO
  - Lakshya Tomar        	UI / Dashboard
  - Ashutosh Agnihotri	    Integration / Testing

## 🏆 Project Status
✅ SIH 2026 Round 1 Qualified
✅ Unity flood environment
✅ Multi-drone simulation
✅ Voice-controlled operations
✅ YOLO-based detection pipeline
✅ Victim tracking system
✅ Rescue dashboard
✅ Training dataset


# 🚧 Further prototype improvements
## 🚀 Future Scope

The project can be extended toward a real-world disaster
response system with:

- Real drone hardware integration
- Edge AI deployment
- Real-time camera feeds
- GPS-based positioning
- Advanced swarm coordination
- Automated route optimization
- Real-time disaster mapping
- Integration with emergency response systems


## ⚠️ Disclaimer

Flood ResQ is currently a simulation/prototype developed for
Smart India Hackathon 2026.

The simulator demonstrates the concept of AI-assisted drone-based
flood rescue and does not directly control real-world rescue drones.

Real-world deployment would require appropriate hardware,
navigation systems, communication infrastructure, safety systems,
testing, and regulatory compliance.

