# SpaceAR

## Overview
SpaceAR is an augmented reality (AR) physics-based jumping game. Built with Unity and Vuforia, it brings a galaxy of planets into your real-world environment. The goal is to navigate an astronaut from planet to planet by calculating the perfect jump.

This game features a complete setup-to-play loop, physics-based trajectory visualization, and a "hold-to-charge" or "ping-pong" power mechanic.

## Features
Augmented Reality: Uses Vuforia's PlaneFinderBehaviour to scan your real-world floor and place game objects.

Dynamic Level Creation: Players place the start, mid-air, and finish planets, creating a unique level every time.

Physics-Based Jumping: Jumps are not canned animations. The astronaut is launched with a calculated velocity and is affected by gravity, requiring skill and timing.

Accurate Trajectory Prediction: A dynamic LineRenderer shows the exact physics-based path the astronaut will take, allowing players to aim their jumps.

Selectable Jump Mechanic: A public boolean useLoopingDistance lets you toggle between:

Ping-Pong Mode (true): The jump power cycles from min to max and back to min, requiring precise timing to release.

Charge-Up Mode (false): Holding the screen charges the jump from min to max.

Life and Win/Loss System: Players have a limited number of lives, and missing a jump (by falling or landing on the AR ground) will cost a life and respawn the player.

Visual Feedback: Planets light up when visited using either a Lens Flare or Material Emission, handled by the GameManager.

## How to Play
The game follows a simple state machine:

PlacingStart: Scan your environment (like a floor or large table). Tap the detected ground plane to place the Start Planet.

CreatingPlanets: Look around and point your phone. Press and hold the screen to create mid-air planets. The size of the planet is determined by how long you hold. Repeat this until all mid-air planets are created.

PlacingFinish: The game will ask you to place the Finish Planet. Tap the ground one last time.

Playing: The game begins! The astronaut appears on the Start Planet.

Press and hold the screen to start your jump.

The trajectory line will appear, showing your path.

The jump power will "ping-pong" or "charge up" based on the useLoopingDistance setting.

Release your touch to jump!

Victory / GameOver: Land on every planet to win! If you run out of lives, the game is over.

Thank you for reading!
