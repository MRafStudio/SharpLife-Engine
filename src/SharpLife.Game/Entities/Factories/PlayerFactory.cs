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

using SharpLife.Engine.Entities;
using SharpLife.Engine.Entities.Components;
using SharpLife.Engine.Entities.Factories;
using SharpLife.Engine.Physics;
using SharpLife.Game.Entities.Components;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpLife.Game.Entities.Factories
{
    /// <summary>
    /// Creates the player.
    /// <remarks>
    /// SharpLife needs a player entity to spawn so the client has something to follow and control.
    /// </remarks>
    /// </summary>
    [LinkEntityToFactory(ClassName = "info_player_start")]
    [LinkEntityToFactory(ClassName = "info_player_deathmatch")]
    public sealed class PlayerFactory : EntityFactory
    {
        protected override void GetComponentTypes(ImmutableHashSet<Type>.Builder types)
        {
            types.Add(typeof(Transform));
            types.Add(typeof(Collider));
            types.Add(typeof(Player));
        }

        public override bool Initialize(EntityCreator creator, Entity entity, IReadOnlyList<KeyValuePair<string, string>> keyValues)
        {
            if (!creator.InitializeComponent(entity.GetComponent<Transform>(), keyValues))
            {
                return false;
            }

            var collider = entity.GetComponent<Collider>();

            if (!creator.InitializeComponent(collider, keyValues))
            {
                return false;
            }

            collider.SetSize(Player.HullMins, Player.HullMaxs);
            collider.Solid = Solid.SlideBox;

            //The engine's physics code doesn't implement MoveType.Walk yet, so the player is moved
            //by PlayerController.Move (driven by the client camera) instead of the entity simulation.
            collider.MoveType = MoveType.None;

            return true;
        }
    }
}
