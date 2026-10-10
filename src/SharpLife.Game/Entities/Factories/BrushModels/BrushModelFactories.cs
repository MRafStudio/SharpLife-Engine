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
using SharpLife.Engine.Models.BSP;
using SharpLife.Engine.Physics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpLife.Game.Entities.Factories.BrushModels
{
    /// <summary>
    /// Base class for brush model entities (doors, buttons, breakables and so on).
    /// These use a BSP sub-model (*N) as their visual and collision model.
    /// </summary>
    public abstract class BrushModelFactory : EntityFactory
    {
        /// <summary>
        /// Whether this brush blocks movement. Purely decorative brushes do not.
        /// </summary>
        protected virtual bool IsSolid => true;

        protected override void GetComponentTypes(ImmutableHashSet<Type>.Builder types)
        {
            types.Add(typeof(Transform));
            types.Add(typeof(Collider));
            types.Add(typeof(RenderProperties));
            types.Add(typeof(BSPRenderableComponent));
        }

        public override bool Initialize(EntityCreator creator, Entity entity, IReadOnlyList<KeyValuePair<string, string>> keyValues)
        {
            if (!creator.InitializeComponent(entity.GetComponent<Transform>(), keyValues))
            {
                return false;
            }

            if (!creator.InitializeComponent(entity.GetComponent<Collider>(), keyValues))
            {
                return false;
            }

            if (!creator.InitializeComponent(entity.GetComponent<RenderProperties>(), keyValues))
            {
                return false;
            }

            if (!creator.InitializeComponent(entity.GetComponent<BSPRenderableComponent>(), keyValues))
            {
                return false;
            }

            //Brush entities must block movement, otherwise maps have holes where a door or a
            //breakable wall should be. func_illusionary is purely decorative and never blocks.
            if (IsSolid)
            {
                var collider = entity.GetComponent<Collider>();
                collider.Solid = Solid.BSP;
                collider.MoveType = MoveType.Push;
            }

            return true;
        }
    }

    //func_door already has a factory of its own (Doors/FuncDoorFactory), which also sets up the
    //door movement logic, so it is intentionally not registered here.

    [LinkEntityToFactory(ClassName = "func_door_rotating")]
    public sealed class FuncDoorRotatingFactory : BrushModelFactory
    {
    }

    [LinkEntityToFactory(ClassName = "func_button")]
    public sealed class FuncButtonFactory : BrushModelFactory
    {
    }

    [LinkEntityToFactory(ClassName = "func_breakable")]
    public sealed class FuncBreakableFactory : BrushModelFactory
    {
    }

    [LinkEntityToFactory(ClassName = "func_pushable")]
    public sealed class FuncPushableFactory : BrushModelFactory
    {
    }

    [LinkEntityToFactory(ClassName = "func_rotating")]
    public sealed class FuncRotatingFactory : BrushModelFactory
    {
    }

    /// <summary>
    /// Decorative brush model: visible but never blocks movement.
    /// </summary>
    [LinkEntityToFactory(ClassName = "func_illusionary")]
    public sealed class FuncIllusionaryFactory : BrushModelFactory
    {
        protected override bool IsSolid => false;
    }
}
