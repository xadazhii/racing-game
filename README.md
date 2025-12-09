# Unity Arcade Racing

**Unity Arcade Racing** is a fast-paced 3D racing game developed with **Unity** and **C#**. The player competes against 3 intelligent AI bots in a 3-lap circuit race. The game features a strategic power-up system, cinematic intro, and a complete lap tracking system.

---

### Table of Contents
- [Visual Overview](#-visual-overview)
- [Game Features](#-game-features)
- [Technical Implementation](#-technical-implementation)
- [Controls](#-controls)
- [Getting Started](#-getting-started)
- [License](#license)

---

### **Visual Overview**

> *Action shot of the race start or a drift mechanic.*



<img width="1449" height="803" alt="Screenshot 2025-12-10 at 00 01 24" src="https://github.com/user-attachments/assets/c42333a4-cb87-413f-92c4-d7531cd5f3fc" />


https://github.com/user-attachments/assets/1f42a181-73f2-464c-baf3-b52a43bf5143


---

### **Game Features**

* **Competitive Racing Loop:** A complete game loop consisting of a cinematic **Intro**, a **3-Lap Race**, and a final **Results/Leaderboard** screen.
* **Intelligent AI:** Includes **3 AI Bots** that navigate the track using Unity's Navigation system (`NavMesh`), providing a challenging race experience.
* **Power-Up System:** Players can collect items on the track to gain an advantage:
    * **Freeze:** Temporarily freezes all opponents in place (`GlobalFreeze`).
    * **Nitro:** Gives a temporary speed boost.
    * **Slow Down:** Traps or debuffs to slow down enemies.
* **Immersive Audio:** Dynamic engine sounds based on speed and background music.

---

### **Technical Implementation**

The project is built using modular C# scripts. Below is an overview of the core architecture based on the project files:

#### **1. Car Physics & Control**
* **`MyCarController.cs`**: Handles the player's input (WASD/Arrows), physics forces for acceleration, steering, and braking.
* **`CarEngineSound.cs`**: Dynamically adjusts the pitch of the engine audio clip based on the car's current RPM/Speed.

#### **2. AI System**
* **`NavAgentFollower.cs`**: Controls the 3 enemy bots. Utilizes Unity's `NavMeshAgent` to calculate paths and follow waypoints around the track efficiently.

#### **3. Race Logic & UI**
* **`CheckpointTrigger.cs`**: Detects when a car passes a checkpoint to prevent cheating and count laps correctly.
* **`CarRaceIdentity.cs`**: Assigns unique IDs to the player and bots to track positions for the leaderboard.
* **`GameIntroManager.cs`**: Manages the pre-race countdown and cinematic camera movements before the race starts.

#### **4. Power-Ups**
* **`FreezePickup.cs`**: Logic for the collectable item on the track.
* **`GlobalFreeze.cs`**: The manager that executes the "Freeze" logic, iterating through all active AI cars and disabling their movement temporarily.

---

### **Controls**

| Key | Action |
| :--- | :--- |
| **W / Up Arrow** | Accelerate |
| **S / Down Arrow** | Brake / Reverse |
| **A / D (Left/Right)** | Steer |
| **Space** | Handbrake |

---
### Getting Started

1.  **Clone the repo:**
    ```bash
    git clone [https://github.com/your-username/unity-racing-game.git](https://github.com/your-username/unity-racing-game.git)
    ```
2.  Open the project in **Unity Hub** (Version 2021.3 LTS or higher recommended).
3.  Navigate to **Scenes** and open **MainTrack**.
4.  Press **Play** to start the race!

---

**License:** 

This project is owned by **Adazhii Kristina**.
