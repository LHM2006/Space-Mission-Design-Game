# 🚀 Orbital Architect — Space Mission Design Game (NASA Space Apps Challenge 2026)

> **Built with Unity (`6000.6.9f1`, 2D URP, C#)** — Interactive spacecraft & mission systems engineering game modeling **NASA CML 1–4**, the **4 Mission Segments** (*Launch, Space, Ground, User*), real CubeSat constraints, Starship launch sequences, and 2D orbital simulation (`1x–128x` time warp).

---

## 🎮 For Judges & Players: How to Start & Play

### Option A: Play Online in Browser (Recommended — No Install Needed)
- **Live Web App (GitHub Pages):** **[https://lhm2006.github.io/Space-Mission-Design-Game/](https://lhm2006.github.io/Space-Mission-Design-Game/)** *(served live from the `gh-pages` branch — works directly in Chrome, Edge, or Firefox with no login required)*
- **Or Run WebGL Locally (`localhost`):** *(Do not open `index.html` directly via `file://`, as browsers block local WebGL `.unityweb` loading.)*
  1. Export WebGL in Unity via **`Space Mission Build` → `2. Build WebGL (localhost & Web)`** (creates `Builds/WebGL`).
  2. Start a local static server:
     ```bash
     python -m http.server 8080 --directory Builds/WebGL
     ```
     *(Or double-click `Builds/Play-Localhost.bat` if included in your local `Builds/` folder.)*
  3. Open **http://localhost:8080** in Chrome, Edge, or Firefox.

### Option B: Download & Run Offline on Windows (`.exe`)
1. Download **[`CubeSatArchitect-Windows.zip`](https://github.com/LHM2006/Space-Mission-Design-Game/releases/download/v1.0.0/CubeSatArchitect-Windows.zip)** (`37.9 MB`) from **[GitHub Release `v1.0.0` (`CubeSatArchitect Release 1.0.0`)](https://github.com/LHM2006/Space-Mission-Design-Game/releases/tag/v1.0.0)** *(or build locally via **`Space Mission Build` → `3. Build Windows (.exe)`**)*.
2. Extract the full `.zip` archive into a folder, keep all extracted files/subfolders together (`UnityPlayer.dll`, `*_Data/`, etc.), and double-click the `.exe` application inside to play.

### Controls & Gameplay Flow (Mouse / Click Only)
1. **Home Screen (`ORBITAL ARCHITECT`):** Pick a mission (**BioNutrients-2 ISS**, **BisonSat 3U SSO**, or **Artemis Lunar Ice Pathfinder**) and click **`SELECT & DESIGN >>`**, or click **`WATCH STARSHIP LAUNCH DEMO`**.
2. **Screen 1 — Spacecraft & Segment Builder (`CML 1–3`):** Switch across **1. Launch**, **2. Space**, **3. Ground**, and **4. User** segments to equip parts, toggle **Phase 3 Pre-Launch Licensing & Testing** (FCC/NOAA, Vibration, TVAC, DITL, Double Burn Wire), and balance the **6 Engineering Resource Margins** (**Mass, Power, Budget, Telecom Link, Science Return, Reliability**) until the **CML 4 RYG Checklist** is ready.
3. **Screen 2 — 2D Orbit & Launch Visualizer (`CML 4`):** Click **`LAUNCH & SIMULATE 2D ORBIT (PHASE 4) >>`** to launch aboard Starship and simulate the orbit (`1x–128x` speed).
4. **Screen 3 — NASA CML Scorecard (`Debrief`):** Review final engineering margins, pass/fail status, and mission results.

---

## 🛰️ NASA Data & Resources Used
- **NASA JPL Concept Maturity Levels** (`13-3547_A1b.pdf`) — CML 1–4 progression & RYG technical margin checklist
- **NASA SmallSat Institute: *Design & Develop Science Missions*** (`250323-design-and-develop-science-mission.pdf`) — 4 mission segments, 6-step lifecycle, BioNutrients-2 constraints
- **NASA CubeSat Launch Initiative: *CubeSat 101*** (`nasa_csli_cubesat_101_508.pdf`) — 1U/3U/6U/P-POD standards and FCC/NOAA/TVAC/Vibration/DITL/Burn Wire mission-killers
- **NASA GMAT R2026a** (`gmat-win-R2026a.zip`) — ISS, Sun-Synchronous Polar (SSO), and Lunar orbital presets

## 🤖 Tech Stack & Use of Super Intelligence (SI / AI) Disclosure
- **Tech Stack:** Unity `6000.6.9f1` (2D Universal Render Pipeline), C#, TextMeshPro, WebGL & Standalone Windows 64-bit targets, Git/GitHub.
- **SI / AI Acknowledgment:** AI coding/engineering assistants were utilized to help accelerate C# boilerplate, UI wiring helpers, documentation, and Unity WebGL/Windows build automation. All challenge concepts, NASA systems-engineering design choices, and final verification were directed and reviewed by the team. No unauthorized NASA logos or branding were generated.

---

# 🛠️ Internal Architecture & Team Guide
**Project Instruction & Master Architecture Guide (For All Team Members & Their Antigravity AI Assistants)**

> **📌 INSTRUCTION FOR TEAMMATES' ANTIGRAVITY AI:**
> If you are an AI assistant reading this file for **Member 1**, **Member 2**, or **Member 3**, please read this entire document carefully.
> 1. **Do NOT generate the entire game at once.** The team consists of Year 2 Software Engineering (SE) students at APU who want to learn and build step-by-step.
> 2. **Follow the exact C# Data Models, Interfaces, and Folder Structure** defined in Section 5 and Section 6 so that all 3 members' code merges cleanly into the shared GitHub repository without merge conflicts.
> 3. Ask your human developer which specific step of their assigned role they want to work on first, explain the concepts clearly, and guide them one file/feature at a time.

---

## 1. Project Overview & Repository
- **Challenge Name:** Space Mission Design Game (NASA Space Apps Challenge 2026)
- **Official Challenge Links:**
  - [Challenge Details](https://www.spaceappschallenge.org/2026/challenges/space-mission-design-game/?tab=details)
  - [Challenge Resources](https://www.spaceappschallenge.org/2026/challenges/space-mission-design-game/?tab=resources)
- **GitHub Repository:** `https://github.com/LHM2006/Space-Mission-Design-Game.git`
- **Tech Stack:** **Unity (2D)** using **C#** (leveraging Object-Oriented Programming concepts from Year 2 SE).
- **Game Style:** **Phase-Based 2D "Mission Architect" Decision & Dashboard Game** (Menu/Card/Dashboard-driven with a 2D Orbit & Launch Visualizer — simple, clean, zero complex 3D physics bugs, and easy to finish within the hackathon).

---

## 2. The Challenge Objective (What NASA Wants)
Space missions are complex systems where scientific goals, engineering requirements, resources, and operational constraints must work together. Improving one area (e.g., adding a high-resolution camera) creates trade-offs in others (increases mass, power consumption, data volume, and cost).

**Our Goal:** Build an interactive 2D Unity game where players design, manage, test, and simulate a complete space mission from concept to operation:
1. **Define Mission Objectives & Requirements:** Pick a science mission (Earth Science, ISS Biology, Lunar Exploration).
2. **Design the Spacecraft:** Select the CubeSat form factor (1U, 3U, 6U) and subsystems (Payload Instruments, Solar Panels, Batteries, Attitude Control [ADCS], Antennas/Comms).
3. **Select Launch Vehicle & Orbit:** Match mass/volume to a rocket/deployer and target orbit (ISS, Low Earth Orbit, Polar/Sun-Synchronous, Lunar).
4. **Manage Engineering Trade-Offs (The 6 Core Metrics):**
   - **Mass (kg)** vs. Max Dispenser/Rocket Limit
   - **Power (W)** Generated vs. Consumed (including eclipse periods)
   - **Budget ($k)** Spent vs. Mission Grant Cap (keeping reserve margins!)
   - **Data Volume (MB/day)** Generated vs. Ground Station Downlink Capacity
   - **Science Return (Score)** vs. Minimum Threshold Requirement
   - **Reliability / Risk (%)** based on testing, licensing, and technical margins
5. **Simulate Mission Outcomes:** Show players *why* their mission succeeded, partially worked, or failed using a **NASA Concept Maturity Level (CML) Scorecard**.

---

## 3. Summary of Official NASA Resources (The Game's Rulebook)
All game mechanics are grounded in the 4 official NASA resource files provided for this challenge:

### Resource 1: JPL Concept Maturity Levels (`13-3547_A1b.pdf`)
- **What it is:** JPL's official framework for evolving a space mission concept in Pre-Phase A/Phase A.
- **How we use it in the game:** Our game stages directly follow **CML 1 to CML 4**:
  - **CML 1 (Cocktail Napkin):** Choose the Mission Scenario & Science Objective.
  - **CML 2 (Initial Feasibility):** Choose the CubeSat size (1U/3U/6U), target orbit, and rough budget class.
  - **CML 3 (Trade Space Exploration):** The Spacecraft Builder! Swap payloads, power, comms, and ground stations while balancing trade-offs. (Players can loop back between CML 2–4 if their margins fail!).
  - **CML 4 (Point Design & RYG Checklist):** Check **Technical Margins** (NASA CML 4 requires healthy margins, e.g., Mass & Power margins, Telecom link margin) and evaluate the **Red / Yellow / Green (RYG) Checklist** (need $\ge 80\%$ Green to pass to launch!).

### Resource 2: NASA Design & Develop Science Missions (`250323-design-and-develop-science-mission.pdf`)
- **What it is:** NASA SmallSat Institute Systems Engineering Webinar ("Begin with the End in Mind").
- **How we use it in the game:**
  - Every mission design is split into the **4 NASA Mission Segments**:
    1. **User Segment:** What science data/samples are needed at the end?
    2. **Space Segment:** The CubeSat bus + science instruments.
    3. **Ground Segment:** Ground station antennas and mission control monitoring.
    4. **Launch Segment:** Launch vehicle / deployer (e.g., NG-18 to ISS, rideshare rocket).
  - Follows the **6-Step Lifecycle**: `Identify Need & Goals` $\rightarrow$ `Define Concept (CONOPS)` $\rightarrow$ `Define Requirements` $\rightarrow$ `Design` $\rightarrow$ `Build & Verify` $\rightarrow$ `Implement`.
  - **Featured Real Mission:** *BioNutrients-2* (growing genetically modified yeast on the ISS to produce beta carotene nutrients for long-duration spaceflight, with strict mass $<250\text{g}$ and volume $<300\text{mL}$ payload constraints).

### Resource 3: NASA CubeSat 101 (`nasa_csli_cubesat_101_508.pdf`)
- **What it is:** NASA CubeSat Launch Initiative (CSLI) guide for student CubeSat developers.
- **How we use it in the game:**
  - **CubeSat Standard Sizes:**
    - **1U:** $10\text{ cm} \times 10\text{ cm} \times 11.35\text{ cm}$, Max Mass $\le 1.33\text{ kg}$
    - **3U:** $10\text{ cm} \times 10\text{ cm} \times 34.05\text{ cm}$, Max Mass $\le 4.00\text{ kg}$
    - **6U:** $20\text{ cm} \times 10\text{ cm} \times 34.05\text{ cm}$, Max Mass $\le 12.00\text{ kg}$
  - **Dispensers:** P-POD (3U deployer), 6U dispensers, and NanoRacks ISS Deployer.
  - **Real-World "Mission Killers" (Used for Phase 4 Review & Testing Events!):**
    - **Regulatory Licensing:** Must apply for **FCC/IARU Radio Frequency (RF) License** and **NOAA Remote Sensing License** (if carrying a camera pointed at Earth). Without them, the CubeSat is **demanifested (scrubbed from launch)**!
    - **Burn Wire Redundancy:** Deployable solar panels/antennas are held down by fishing line and a resistor ("burn wire"). Using a **Double Burn Wire** prevents premature release during launch vibration or failure to deploy in orbit.
    - **UL-Listed Batteries & Inhibits:** Using certified batteries + Remove Before Flight (RBF) pin + separation switches prevents catastrophic electrical failure.
    - **Environmental Verification Testing:**
      - *Vibration/Shock Testing:* Ensures parts don't shake loose on the rocket.
      - *Thermal Vacuum (TVAC) Bakeout:* Prevents material **outgassing** from fogging up the camera lens in space.
      - *Day-In-The-Life (DITL) Test:* Verifies flight software timers (e.g., waiting 30–45 mins after P-POD ejection before deploying antennas and transmitting).
    - **Orbital Debris (ODAR):** Avoiding high-melting-point metals (like titanium/tungsten) in favor of aluminum so the CubeSat burns up safely on re-entry within 25 years.

### Resource 4: NASA GMAT R2026a (`gmat-win-R2026a.zip`)
- **What it is:** NASA's General Mission Analysis Tool for orbital mechanics.
- **How we use it in the game:** Instead of forcing players to learn complex orbital differential equations, we use pre-calculated GMAT orbital presets (ISS Orbit $\sim 400\text{ km}$, Sun-Synchronous Polar Orbit $\sim 600\text{ km}$, Geostationary Transfer / Lunar Orbit) with realistic sunlight/eclipse ratios, ground pass durations, and radiation/delta-V requirements!

---

## 4. The 5-Phase Gameplay Flow (How the Game Plays)

```
[Phase 1: CML 1 - Mission Selection]
  Player picks 1 of 3 NASA Missions (e.g., BioNutrients-2 ISS, BisonSat Earth Imager, Lunar Ice Finder).
  Displays: Budget Cap, Target Science Score, Required Orbit, and Special Constraints.
        │
        ▼
[Phase 2: CML 2 & 3 - Spacecraft & Segment Builder (Trade Space)]
  Player selects components across the 4 Mission Segments:
  1. Launch Segment: CubeSat Chassis (1U/3U/6U) + Rocket/Deployer (ISS Resupply, Rideshare LEO, Dedicated Rocket)
  2. Space Segment: Science Payload(s), Solar Panels, Battery, ADCS (Magnetorquers vs. Reaction Wheels), Comms Transceiver
  3. Ground Segment: Amateur UHF Ground Station vs. University S-Band Dish vs. NASA Network
  4. User Segment: Data Processing & Science Traceability Plan
  * Live UI Bars update immediately: Mass, Power, Budget, Data Link, Science Score, Technical Margin! *
        │
        ▼
[Phase 3: CML 4 - Pre-Launch Verification, Licensing & Dilemma Cards]
  Player allocates remaining budget/time to pass NASA Readiness Reviews:
  - Regulatory Check: FCC/IARU RF License & NOAA Camera License
  - Testing Check: Vibration Test, TVAC Bakeout (Outgassing), DITL Software Test
  - Engineering Dilemma Cards (2-3 interactive choices, e.g., "Double up on burn wires?", "Swap to low-outgassing adhesive?")
  - Live CML 4 RYG Checklist shows Red / Yellow / Green status across Science, Technical, Management, and Cost.
        │
        ▼
[Phase 4: Launch & On-Orbit Simulation (2D Visualizer)]
  2D animated sequence showing:
  1. Rocket Ascent & Vibration check
  2. Dispenser Ejection & 45-min DITL Timer
  3. Solar Panel / Antenna Burn-Wire Deployment
  4. Orbiting Earth/Moon, collecting Science Data during sunlight, surviving Eclipse on Battery, and beaming data to the Ground Station!
        │
        ▼
[Phase 5: Mission Debrief & NASA CML Scorecard]
  Final Grade (S / A / B / C / Mission Scrubbed / Lost in Space).
  Detailed educational breakdown explaining WHY each subsystem succeeded or failed based on real NASA Systems Engineering rules!
```

---

## 5. Team Roles & Task Assignments (3 Year-2 SE Students)

To prevent Git merge conflicts in Unity, **each member works in their own dedicated folder and scenes/scripts**:

### 👨‍💻 Member 1: Systems & Simulation Engine Lead (Backend / C# Logic & Data)
- **Primary Focus:** The "Brain" — OOP Data Models, Parts Catalog, Trade-Off Math Calculator, and CML 4 RYG Checklist Evaluator.
- **Folder Ownership:** `Assets/_Project/Scripts/Core/` and `Assets/_Project/Scripts/Data/`
- **Specific Tasks:**
  1. Create the C# data classes (`ComponentPart`, `MissionProfile`, `SpacecraftDesign`, `SimulationResult`).
  2. Create the **Parts Catalog** (`PartsDatabase.cs`) with realistic stats from *CubeSat 101* and *GMAT* (frames, payloads, solar panels, batteries, radios, rockets, orbits, ground stations).
  3. Write the `MissionCalculator.cs` class that calculates:
     - Total Mass vs. Max Chassis/Launch Mass + **Mass Margin (%)**
     - Power Generated (adjusted for Orbit sunlight %) vs. Power Consumed + **Power Margin (%)**
     - Data Generated (MB/orbit) vs. Ground Station Downlink (MB/pass)
     - Total Cost vs. Budget Cap + **Cost Reserve (%)**
     - CML 4 RYG Status (`Green`, `Yellow`, `Red`) for each requirement.
- **Does NOT touch:** UI Canvas layouts or story event dialogs (provides clean C# methods for Member 2 and Member 3 to call).

---

### 🎨 Member 2: Interactive UI & 2D Visualizer Lead (Frontend / Unity UI & Animation)
- **Primary Focus:** The "Cockpit" — Unity 2D Canvas Dashboards, Part Selector Cards, Live Resource Meters, and the 2D Orbit Simulation View.
- **Folder Ownership:** `Assets/_Project/Scripts/UI/`, `Assets/_Project/Scenes/`, and `Assets/_Project/Art/`
- **Specific Tasks:**
  1. Build the **Spacecraft Builder Dashboard UI** (tabs for Launch, Space, Ground, User segments; clickable part cards; visual 2D preview of the 1U/3U/6U CubeSat slots).
  2. Build the **Live Resource Bars Panel** (Mass, Power, Budget, Data, Science, Margin) that turns Green/Yellow/Red using Member 1's `MissionCalculator`.
  3. Build the **2D Orbit & Launch Simulation View** (a clean 2D rotating Earth/Moon, an orbiting CubeSat sprite, day/night shadow zone, ground station signal cone, and step-by-step flight log ticker).
- **Does NOT touch:** Core math formulas or writing all the mission/event text data.

---

### 🎬 Member 3 (HanMing): Game Progression, Scenarios & Integration Lead (Full-Stack / Content & Pitch)
- **Primary Focus:** The "Director" — Game State Flow (CML 1 $\rightarrow$ 5), Playable Mission Scenarios, CubeSat 101 Review/Dilemma Events, Final Scorecard, Git Integration, and Space Apps Pitch.
- **Folder Ownership:** `Assets/_Project/Scripts/Managers/`, `Assets/_Project/Scripts/Scenarios/`, and Documentation/Pitch assets.
- **Specific Tasks:**
  1. **Game State Manager (`GameManager.cs`):** Control the progression across the 5 Phases (Title $\rightarrow$ Mission Select $\rightarrow$ Builder $\rightarrow$ Review/Testing Events $\rightarrow$ Orbit Sim $\rightarrow$ Debrief Scorecard).
  2. **3 Playable NASA Mission Scenarios (`MissionScenariosData.cs`):**
     - *Mission 1 (Tutorial/Easy):* **BioNutrients-2 (ISS Research Mission)** — Focus on mass/volume limits, crew safety, and biological sample incubation.
     - *Mission 2 (Medium):* **BisonSat / Earth Forest Imager (3U LEO CubeSat)** — Focus on NOAA camera licensing, ADCS pointing accuracy, data downlink, and TVAC outgassing.
     - *Mission 3 (Hard):* **Artemis Lunar Ice Pathfinder (6U Deep Space CubeSat)** — Focus on tight $\Delta v$/propulsion, high radiation, deep-space telecom margin, and strict 35% NASA CML 4 margins.
  3. **Review Gate & Dilemma Event System (`ReviewEventManager.cs`):** Create the interactive Pre-Launch Review & Testing phase (FCC/NOAA licensing, Vibration test, TVAC bakeout, DITL test, Burn-wire dilemma cards) and calculate how the player's choices affect mission survival in Phase 4.
  4. **Final CML Scorecard & Hackathon Pitch:** Build the Debrief UI logic that explains *why* the player won/lost using real NASA terminology, manage the GitHub repo merges, and prepare the Space Apps presentation/video.

---

## 6. Shared C# Architecture Contract (How Our Code Connects)

To make sure all 3 members can code independently and plug everything together smoothly, we use **plain C# classes / structs** in a shared namespace `SpaceMissionGame.Core`.

### Proposed Unity Folder Structure
```text
Assets/
└── _Project/
    ├── Scenes/
    │   └── MainGame.unity          (Single-scene panel switching = easiest & zero scene-loading bugs!)
    ├── Scripts/
    │   ├── Core/                   <-- [Member 1] Math, Calculators, Enums, Data Models
    │   ├── Data/                   <-- [Member 1] Parts Database (Spacecraft parts, Orbits, Rockets)
    │   ├── UI/                     <-- [Member 2] Dashboard UI, Resource Bars, 2D Orbit Visualizer
    │   ├── Managers/               <-- [Member 3] GameManager, Phase Controller, Audio/Event flow
    │   └── Scenarios/              <-- [Member 3] Mission Profiles, Dilemma Cards, Review Gate Logic
    ├── Prefabs/
    └── Art/
```

> **💡 Why Single-Scene Panel Switching (`MainGame.unity`)?**
> Instead of 5 separate Unity scenes that lose data when switching scenes, we keep 1 `GameManager` and 5 UI Panel GameObjects (`MissionSelectPanel`, `BuilderPanel`, `ReviewPanel`, `SimulationPanel`, `ScorecardPanel`) that turn on/off via `SetActive(true/false)`. To avoid Git conflicts on `MainGame.unity`, Member 2 & Member 3 can save each panel as a **Prefab** in `Assets/_Project/Prefabs/`!

### Core Enums & Data Structures (Shared Reference)
```csharp
namespace SpaceMissionGame.Core
{
    // Maps to JPL Concept Maturity Levels (13-3547_A1b.pdf)
    public enum GamePhase
    {
        MainMenu,
        CML1_MissionSelect,       // Cocktail Napkin: Pick Science Mission
        CML2_3_SpacecraftBuilder, // Initial Feasibility & Trade Space: Pick Parts & Segments
        CML4_ReviewAndTesting,    // Point Design: Checklist, Licensing & Dilemma Events
        PhaseCD_OrbitSimulation,  // Launch & Operations Simulation
        PostMission_Scorecard     // Final NASA Debrief & Grade
    }

    // Maps to NASA Science Mission Segments (250323-design-and-develop-science-mission.pdf)
    public enum MissionSegment
    {
        Launch, // Chassis size (1U/3U/6U) & Launch Vehicle / Dispenser
        Space,  // Payload, Power (Solar/Battery), ADCS, Comms
        Ground, // Ground Station Antenna & Operations
        User    // Science Data System & Traceability
    }

    public enum PartCategory
    {
        Chassis,        // 1U, 3U, 6U
        LaunchVehicle,  // ISS Cargo, Rideshare LEO, Lunar Trajectory
        Payload,        // Cameras, Spectrometers, Bio-Incubator
        PowerSystem,    // Body-mounted Solar, Deployable Solar, Batteries
        ADCS,           // Passive Magnet, Magnetorquers, 3-Axis Reaction Wheels
        Communications, // Amateur UHF, S-Band, X-Band Deep Space
        GroundStation   // Amateur Yagi, University Dish, NASA Near Space Network
    }

    // Red / Yellow / Green status from JPL CML 4 Checklist
    public enum RYGStatus
    {
        Red,    // Failed / Critical Deficiency
        Yellow, // Marginal / Risk of Failure
        Green   // Compliant with NASA Standards
    }
}
```

---

## 7. Git Workflow Rules for Unity (Crucial for 3 Teammates!)
Unity projects can have messy Git conflicts if two people edit the same `.unity` scene file at the same time. Follow these 4 golden rules:
1. **Use `.gitignore` for Unity:** Never commit the `Library/`, `Temp/`, `Obj/`, `Logs/`, or `UserSettings/` folders! Only commit `Assets/`, `Packages/`, and `ProjectSettings/`.
2. **Force Text Serialization:** In Unity, go to `Edit -> Project Settings -> Editor`:
   - Set **Asset Serialization Mode** to `Force Text`.
   - Set **Version Control Mode** to `Visible Meta Files`.
3. **Work in Separate Scripts & Prefabs:**
   - **Member 1** only edits scripts in `Scripts/Core/` and `Scripts/Data/`.
   - **Member 2** only edits scripts in `Scripts/UI/` and UI Prefabs.
   - **Member 3** only edits scripts in `Scripts/Managers/` and `Scripts/Scenarios/`.
4. **Pull Before You Push:** Always run `git pull --rebase` before starting work and commit small, working steps.

---

## 8. Step-by-Step Roadmap (How We Will Build This Together)
Remember: **Do not let AI generate the whole project blindly.** Each member should follow these steps with their Antigravity assistant:

- **Step 1: Repository & Unity Project Setup** (Clone repo, add Unity `.gitignore`, set up the `Assets/_Project/` folder structure).
- **Step 2: Shared Core Data Models** (Define the shared enums and data classes so all 3 members have the exact same foundation).
- **Step 3: Role-Specific Implementation (One script at a time!)**
  - *Member 1:* Parts Database $\rightarrow$ Trade-off Calculator $\rightarrow$ CML 4 RYG Evaluator.
  - *Member 2:* Builder UI Canvas $\rightarrow$ Resource Progress Bars $\rightarrow$ 2D Orbit Visualizer.
  - *Member 3:* `GameManager` Phase Switcher $\rightarrow$ 3 NASA Mission Scenarios $\rightarrow$ CubeSat 101 Review/Testing Events $\rightarrow$ Final Debrief Scorecard.
- **Step 4: Integration & Playtesting** (Connect the UI buttons to the Managers and Calculators).
- **Step 5: NASA Space Apps Pitch & Video** (Highlight how JPL CMLs, NASA SmallSat SE, CubeSat 101, and GMAT power our game).
