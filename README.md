## Introduction:

This report focuses on the development of a multiplayer first-person shooter (FPS) game using Unity game engine and Mirror networking framework. Our team's project aims to create an immersive and thrilling gaming experience by enabling multiple players to participate concurrently in a real-time game environment. The game design we have implemented revolves around the classic FPS genre, known for its intense action and strategic gameplay.
The core concept of the game centers around players engaging in combat within a virtual world. By utilizing Unity's robust features and Mirror networking framework, we have established a seamless and synchronized multiplayer experience, allowing players to interact, cooperate, or compete with one another. The game leverages the power of networking to facilitate smooth communication between players, ensuring that actions and events are accurately replicated across all connected clients.

[Project Files](https://github.com/gulraiznoorbari/Death-Match-3D/tree/main/Assets) (For Complete Unity Project Files)

## Working

### Mirror Networking Functions:

1. **_NetworkManager_**: The **_NetworkManager_** is a fundamental component provided by Mirror networking. It manages networked gameplay and handles key functionalities such as spawning players, syncing network objects, and managing network connections. It allows clients to connect to a server and handles the process of synchronizing game state between all connected players.
2. **_NetworkBehaviour_**: This is a base class for networked components in Mirror networking. By inheriting from **_NetworkBehaviour_**, scripts can implement networked functionality for game objects. Networked behaviors can have variables and methods that are automatically synchronized across the network, allowing for seamless communication between clients.
3. **_Command_** and **_ClientRPC_**: These are attributes used in conjunction with methods inside NetworkBehaviour scripts. **_Command_** is used for methods that are called by clients to send a command to the server, while **_ClientRPC_** is used for methods that are called by the server to send information to all clients. These attributes ensure that the method calls are executed correctly across the network.
4. **_NetworkIdentity_**: This component is used to identify networked game objects. It provides a unique identifier for each object, allowing clients to recognize and synchronize the object's state. **_NetworkIdentity_** is automatically added to game objects when they are spawned in the networked scene.
5. **_SyncVar_**: This attribute is used to mark variables inside NetworkBehaviour scripts that should be synchronized across the network. When the value of a **_SyncVar_** variable changes on the server, it automatically updates on all clients connected to the server. This ensures that game state changes are propagated accurately to all players.
6. **_NetworkTransform_**: This component is used to synchronize the position and rotation of networked game objects across all clients. It automatically interpolates and predicts movement to ensure smooth synchronization between different networked instances of the same object.

### Game Functions:

#### Start()

-   This function is called when the script is first enabled.
-   It initializes various variables and references, and sets up the FPS camera for the local player.
-   It also sets up the UI elements and cursor for the local player.

#### Update()

-   This function is called every frame.
-   It handles the player's input, such as mouse movement, shooting, reloading, and key presses.
-   It also checks if the player is grounded and applies movement and drag forces accordingly.
-   It calls other functions based on the player's actions.

#### FixedUpdate()

-   This function is called at a fixed interval (physics update).
-   It handles the player's movement by applying forces to the rigidbody component based on the player's input and state.

#### StateHandler()

-   This function determines the current movement state of the player (walking, crouching, sprinting, jumping, wall running).
-   It sets the appropriate movement speed and state based on the player's input and conditions.

#### MouseLookAround()

-   This function handles the rotation of the player's camera based on mouse input.
-   It calculates the rotation values based on the mouse movement and applies them to the camera and player orientation.

#### MyInput()

-   This function reads the player's input for movement, jumping, crouching, and sprinting.
-   It also triggers the corresponding actions based on the input.

#### MovePlayer()

-   This function applies movement forces to the player based on the input and current state.
-   It determines the direction of movement based on the player's orientation and input.
-   It applies different forces for walking and jumping.

#### SpeedControl()

-   This function limits the player's velocity to the maximum move speed.
-   It checks the current velocity and reduces it if it exceeds the limit.

#### StartCrouch()

-   This function handles the player starting to crouch.
-   It changes the player's scale to make them shorter and applies downward force.
-   If the player is moving, it applies an additional forward force.

#### StopCrouch()

-   This function handles the player stopping crouching.
-   It restores the player's scale to the original height.

#### Jump()

-   This function handles the player jumping.
-   It resets the player's vertical velocity and applies an upward force.

#### ResetJump()

-   This function resets the "ready to jump" flag to allow the player to jump again after a cooldown period.

#### Shoot()

-   This function is called when the player shoots their weapon.
-   It checks if the player can shoot based on their ammo count and if they are alive.
-   It then invokes a command to execute the shooting logic on the server.

#### CmdTryShoot()

-   This command function is executed on the server to perform shooting logic.
-   It checks if the player can shoot based on their ammo count and if they are alive.
-   If conditions are met, it reduces the ammo count, performs a raycast to detect hits, and applies damage to the hit target.

#### TargetShoot()

-   This target RPC function is executed on the player who successfully shot.
-   It updates the UI to reflect the new ammo count.

##### RpcPlayerFired()

-   This RPC function is executed on all clients when a player fires their weapon and hits an object other than a player.
-   It spawns bullet hole and impact effects at the hit position and plays the muzzle flash effect.

#### RpcPlayerFiredEntity()

-   This RPC function is executed on all clients when a player fires their weapon and hits another player.
-   It spawns bullet hole and blood impact effects on the hit player and plays the muzzle flash effect.

#### MuzzleFlash()

-   This function plays the muzzle flash effect on the weapon.

#### Reload()

-   This function handles the player reloading their weapon.
-   It checks if the player can reload based on their ammo count and if they are alive.
-   It invokes a command to execute the reloading logic on the server.

#### CmdReload()

-   This command function is executed on the server to perform the reloading logic.
-   It sets the ammo count to the maximum value and updates the UI.

#### TargetReload()

-   This target RPC function is executed on the player who successfully reloaded.
-   It updates the UI to reflect the new ammo count.

#### TakeDamage()

-   This function handles the player taking damage.
-   It reduces the player's health based on the amount of damage received.
-   If the player's health reaches zero, it invokes a command to execute the death logic on the server.

#### CmdPlayerDied()

-   This command function is executed on the server when a player's health reaches zero.
-   It sets the player's health to zero, invokes a respawn command, and updates the UI.

#### CmdPlayerRespawn()

-   This command function is executed on the server to respawn a player.
-   It resets the player's health, ammo count, and position.

#### RpcPlayerRespawned()

-   This RPC function is executed on all clients when a player respawns.
-   It updates the UI and resets the player's position.
