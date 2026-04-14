# Dead Last

**Dead Last** is a fast-paced 3D top-down zombie survival shooter with roguelike elements. The game challenges players to survive against an unrelenting and evolving horde of undead in a high-stakes, run-based environment.

## Genre
**Genre:** 3D Top-Down Shooter / **Roguelike** / Survival Horror


## Core Idea
The player is dropped into an arena where they must fend off waves of zombies that grow more aggressive the closer they get. As a **roguelike**, every session is a unique run where the player must balance combat skill with scaling difficulty, knowing that one mistake could end their progress permanently.

## Mechanics
* **Player Movement:** Navigate the environment using WASD controls. The character utilizes auto-rotation to align with movement direction or targets.
* **Verticality:** The ability to jump allows the player to vault over or onto obstacles to create distance from the horde.
* **Combat System:** Mouse-based projectile shooting. Players must aim and fire to thin out the encroaching threats.
* **Roguelike Elements:**
    * **Permadeath:** Once the player's health reaches zero, the current run ends, and the player must start fresh.
    * **Run-Based Progression:** Success is measured by how far you can push through the escalating difficulty in a single life.
* **Dynamic Zombie AI:** * **Proximity Scaling:** Enemies adapt their speed based on their distance to the player, transitioning from a walk to a run and finally a sprint as they close the gap.
    * **State-Based Animations:** Zombies feature dedicated hit-reaction and death animations for visual feedback.
* **Difficulty Scaling:** The game dynamically increases the zombie spawn rate and the maximum population cap as the session progresses, ensuring no two runs feel the same.

## Win/Lose Conditions
* **Lose Condition:** The run ends immediately when the player’s health bar is fully depleted.
* **Win/Progress Condition:** Survival is the primary objective. The goal is to achieve the highest possible score and survive for the longest duration against the infinite, scaling horde. 

## UI Plan
The interface is designed to provide essential survival data while supporting the roguelike loop:
* **Player HUD:** Includes a prominent health bar and a real-time score tracker to monitor current run progress.
* **World-Space UI:** Enemy health bars are positioned directly above individual zombies to help players prioritize targets.
* **Menu Systems:** * **Main Menu:** Initial entry point for starting a new run.
    * **Game Over Screen:** Triggered upon death, displaying the final score, upgrades gained throughout the run, run duration, and the option to "Try Again" to start a new session.
