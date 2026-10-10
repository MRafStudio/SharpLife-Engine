/***
*
*	Copyright (c) 1996-2001, Valve LLC. All rights reserved.
*	
*	This product contains software technology licensed from Id 
*	Software, Inc. ("Id Technology").  Id Technology (c) 1996 Id Software, Inc. 
*	All Rights Reserved.
*
*   This source code contains proprietary and confidential information of
*   Valve LLC and its suppliers.  Access to this code is restricted to
*   persons who have executed a written SDK license with Valve.  Any access,
*   use or distribution of this code by or to any unlicensed person is illegal.
*
****/

using System.Numerics;

namespace SharpLife.Engine.Entities.Components
{
    /// <summary>
    /// Base class for components that turn an entity into a player.
    /// <remarks>
    /// The engine itself doesn't know how the game implements its player, so the client camera
    /// only relies on this contract to follow and drive the local player.
    /// </remarks>
    /// </summary>
    public abstract class PlayerController : Component
    {
        /// <summary>
        /// World-space position of the player's eyes, used as the camera position
        /// </summary>
        public abstract Vector3 EyePosition { get; }

        /// <summary>
        /// Moves the player for one frame.
        /// <paramref name="forward"/> and <paramref name="right"/> are unit vectors in the horizontal plane.
        /// </summary>
        /// <param name="world">The entity scene to run physics against</param>
        /// <param name="deltaSeconds">Frame time</param>
        /// <param name="forwardMove">Forward/backward input in the range -1..1</param>
        /// <param name="sideMove">Left/right input in the range -1..1</param>
        /// <param name="jump">Whether the jump key is held down</param>
        public abstract void Move(Scene world, float deltaSeconds, in Vector3 forward, in Vector3 right, float forwardMove, float sideMove, bool jump);
    }
}
