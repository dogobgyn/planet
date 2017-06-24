using UnityEngine;
using System.Collections;
using System;

namespace Planet
{
    public class PVoxelLight : TwoDee.VoxelLight, IBlueprintPlaced
    {
        public float m_PowerDrain;
        void IBlueprintPlaced.BlueprintPlaced(BlueprintPlaceArgs args)
        {
            RegenerateContribution();
        }
        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (m_PowerDrain > 0.0f)
            {
                Emitting = PowerConduit.DrainEnergy(gameObject, dt, true);
            }
        }
    }

}