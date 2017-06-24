using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Networking;
using TwoDee;

namespace Planet
{
    public class WorldState : NetworkBehaviour
    {
        bool m_FreezeTime;
        // In units of hours
        float m_TimeGMT;
        public float m_SecondsPerGameHour = 60.0f;
        public float m_StartingTimeGMT = 10.0f;
        public Light m_Sunlight;
        float m_OrigIntensity;
        Quaternion m_OrigRotate;
        float m_SpeedTime;

        public float SpeedTimeLeft
        {
            get
            {
                return m_SpeedTime;
            }
        }
        public void SpeedTime(float time)
        {
            m_SpeedTime += time;
        }
        public void SpeedTimeMinutes(float time)
        {
            // Time is in hours
            m_SpeedTime += time / 60.0f;
        }

        public static WorldState Instance
        {
            set; get;
        }
        public float CurrentTimeAt(Vector3 place_ws)
        {
            float angle = Mathf.Atan2(place_ws.y, place_ws.x);
            float anglePct = (1.75f + (angle / (Mathf.PI * 2.0f))) % 1.0f;
            if (anglePct > 0.5f) anglePct += -1.0f;

            float hourOffsetAtPosition = anglePct;
            return m_TimeGMT + 24.0f * hourOffsetAtPosition;
        }
        public float CurrentTimeGMT
        {
            get
            {
                return m_TimeGMT;
            }
        }
        public float CurrentTime
        {
            get
            {
                return CurrentTimeAt(Camera.main.transform.position);
            }
        }
        public void Start()
        {
            Instance = this;

            m_TimeGMT = m_StartingTimeGMT;
            if (m_Sunlight != null)
            {
                m_OrigIntensity = m_Sunlight.intensity;
                m_OrigRotate = m_Sunlight.transform.rotation;
            }
        }

        public float SunlightIntensity
        {
            set; get;
        }

        public bool IsNightTime(Vector3 at_ws)
        {
            float localTime = CurrentTimeAt(at_ws);
            var currentHour24hr = localTime % 24.0f;
            if (currentHour24hr < 6 || currentHour24hr > 21) return true;
            return false;
        }

        public GameObject m_NightSpawnPrefab;
        float m_NextNightSpawn = 0.0f;
        GameObject m_CurrentNightSpawn;
        public void SpawnNightEnemies(float dt)
        {
            var player = TwoDee.ComponentList.GetFirst<ThirdPersonUserControl>();
            if (player == null) return;

            var playerPos = player.transform.position;

            if (m_CurrentNightSpawn != null)
            {
                m_NextNightSpawn = 0.0f;
                var pos = m_CurrentNightSpawn.transform.position;
                var distance = (pos - playerPos).magnitude;
                if (distance > 30.0f)
                {
                    Destroy(m_CurrentNightSpawn);
                    m_CurrentNightSpawn = null;
                }
            }
            else
            {
                if (IsNightTime(playerPos)) m_NextNightSpawn += dt;

                var vgen = ComponentList.GetFirst<PVoxelGenerator>();
                Vector3 pos = playerPos + Vector3.up * 6.0f;
                if (vgen.IsBoxClearAt(pos, Vector3.up, 0.1f, 0.1f) && !vgen.IsStartPoint(pos))
                {
                    if (m_NextNightSpawn > 20.0f)
                    {
                        m_NextNightSpawn = 0.0f;
                        if (m_NightSpawnPrefab != null)
                        {
                            m_CurrentNightSpawn = GameObject.Instantiate<GameObject>(m_NightSpawnPrefab, pos, Quaternion.identity);
                            NetworkServer.Spawn(m_CurrentNightSpawn);
                        }
                    }
                }
            }

        }

        public void FixedUpdate()
        {
            //@TEMP
            //SpawnNightEnemies(Time.fixedDeltaTime);
        }

        public void Update()
        {
            if (!m_FreezeTime)
            {
                float timeToPass = Time.deltaTime * (1.0f / m_SecondsPerGameHour);
                if (m_SpeedTime > 0.0f)
                {
                    float timeFactor = 5.0f + m_SpeedTime * 200.0f;

                    timeToPass *= timeFactor;
                    m_SpeedTime -= timeToPass;
                }
                m_TimeGMT += timeToPass;
            }

            float localTime = CurrentTime % 24.0f;
            float intensityPercent = (new float[] { 0.0f, 0.0f, 6.0f, 0.0f, 8.0f, 1.0f, 18.0f, 1.0f, 22.0f, 0.0f, 24.0f, 0.0f }).SmoothPairInterp(localTime);
            SunlightIntensity = intensityPercent;

            if (m_Sunlight != null)
            {
                m_Sunlight.intensity = intensityPercent * m_OrigIntensity;
                var rot = TwoDee.Math3d.FromLengthAngleDegrees2D(1.0f, -270.0f + 360.0f * (localTime / 24.0f));
                rot.z = 1.0f;
                m_Sunlight.transform.rotation = Quaternion.FromToRotation(Vector3.forward, rot.normalized);

                // @TEMP
                m_Sunlight.enabled = false;
            }
        }
    }
}