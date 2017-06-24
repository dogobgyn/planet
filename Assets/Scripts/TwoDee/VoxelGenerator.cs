using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace TwoDee
{
    [Serializable]
    public class Rle2DArrayProxy
    {
        int m_X;
        int m_Y;
        public string m_Data;

        public void AppendConsecutive(StringBuilder builder, int consecutiveValue, int numConsecutiveValues)
        {
            if (numConsecutiveValues == 0) return;
            builder.AppendFormat("{0}_{1}\n", consecutiveValue, numConsecutiveValues);
        }

        public void Save(byte[,] values)
        {
            var saveData = new int[values.GetLength(0), values.GetLength(1)];
            for (int y = 0; y < values.GetLength(1); y++)
                for (int x = 0; x < values.GetLength(0); x++)
                    saveData[x, y] = values[x, y];

            Save(saveData);
        }

        public void Save(float[,] values)
        {
            var saveData = new int[values.GetLength(0), values.GetLength(1)];
            for (int y = 0; y < values.GetLength(1); y++)
                for (int x = 0; x < values.GetLength(0); x++)
                    saveData[x, y] = Mathf.RoundToInt(255 * (values[x, y]));

            Save(saveData);
        }

        public void Save(int[,] values)
        {
            m_X = values.GetLength(0);
            m_Y = values.GetLength(1);

            StringBuilder builder = new StringBuilder();
            int consecutiveValue = 0;
            int numConsecutiveValues = 0;

            for (int y = 0; y < values.GetLength(1); y++)
                for (int x = 0; x < values.GetLength(0); x++)
                {
                    int value = values[x, y];
                    if (numConsecutiveValues == 0)
                    {
                        consecutiveValue = value;
                        numConsecutiveValues = 1;
                    }
                    else
                    {
                        if (value == consecutiveValue)
                        {
                            numConsecutiveValues++;
                        }
                        else
                        {
                            AppendConsecutive(builder, consecutiveValue, numConsecutiveValues);

                            consecutiveValue = value;
                            numConsecutiveValues = 1;
                        }
                    }

                }

            AppendConsecutive(builder, consecutiveValue, numConsecutiveValues);
            m_Data = builder.ToString();
        }

        public byte[,] LoadByte()
        {
            byte[,] output = new byte[m_X, m_Y];
            var loadData = LoadInt();
            for (int y = 0; y < output.GetLength(1); y++)
                for (int x = 0; x < output.GetLength(0); x++)
                    output[x, y] = (byte)loadData[x, y];

            return output;
        }

        public float[,] LoadFloat()
        {
            float[,] output = new float[m_X, m_Y];
            var loadData = LoadInt();
            
            for (int y = 0; y < output.GetLength(1); y++)
                for (int x = 0; x < output.GetLength(0); x++)
                    output[x, y] = loadData[x, y] / 255.0f;

            return output;
        }

        public int[,] LoadInt()
        {
            int[,] output = new int[m_X, m_Y];
            using (StringReader reader = new StringReader(m_Data))
            {
                int x = 0;
                int y = 0;
                for (;;)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] subStrings = line.Split('_');
                    int value = int.Parse(subStrings[0]);
                    int count = int.Parse(subStrings[1]);

                    for (int j = 0; j < count; j++)
                    {
                        output[x, y] = value;
                        x++;
                        if (x >= output.GetLength(0))
                        {
                            x = 0;
                            y++;
                        }
                    }
                }
            }

            return output;
        }
    }
    public class GridPointsDirtyArgs
    {
        public GridPointsDirtyArgs(VoxelGenerator gen, Vector3 ll, Vector3 ur)
        {
            m_LowerLeft_ws = ll;
            m_UpperRight_ws = ur;
            m_Gen = gen;
        }

        public VoxelGenerator m_Gen;
        public Vector3 m_LowerLeft_ws;
        public Vector3 m_UpperRight_ws;

        public Bounds DirtyBox_ws
        {
            get
            {
                var size = (m_UpperRight_ws - m_LowerLeft_ws);
                size.z = 10.0f;
                return new Bounds((m_LowerLeft_ws + m_UpperRight_ws) / 2.0f, size);
            }
        }
    }

    public interface IGridPointsDirty
    {
        void GridPointsDirty(GridPointsDirtyArgs args);
    }

    /*
    Keeping this around in case it's still needed later, the base Xiaolin Wu line drawing algorithm
                public bool plot(int x, int y, float coverage, ref Vector3 hitLoc_gs)
            {
                var start = m_Gen.transform.TransformPoint(new Vector3(x / (1.0f * m_Dimension.X), y / (1.0f * m_Dimension.Y)));
                var end = m_Gen.transform.TransformPoint(new Vector3((x+1) / (1.0f * m_Dimension.X), (y+1) / (1.0f * m_Dimension.Y)));
                Debug.DrawLine(start, end, (GetValue(x, y) > 0.5f) ? Color.white : Color.cyan);
                if (GetValue(x, y) > 0.5f)
                {
                    hitLoc_gs = new Vector3(x, y);
                    return true;
                }
                return false;
            }
            public int round(float x)
            {
                return ipart(x + 0.5f);
            }
            public float fpart(float x)
            {
                if (x < 0.0f) return 1.0f - (x - Mathf.Floor(x));
                return x % 1.0f;
            }
            public int ipart(float x)
            {
                return Mathf.FloorToInt(x);
            }

            public float rfpart(float x)
            {
                return 1.0f - (x % 1.0f);
            }
            public void swap(ref float a, ref float b)
            {
                float temp = a;
                a = b;
                b = temp;
            }
            public bool IntersectSegment(Vector3 v0, Vector3 v1, out Vector3 hitLoc_gs)
            {
                // Similar to the XiaoLin Wu Algorithm except we care about order of plotting pixels.
                hitLoc_gs = new Vector3();
                // Use XiaoLin Wu Algorithm to loop through each cell in order the line passes through.  Do a poly check with the line for each cell hit.
                float x0 = v0.x, y0 = v0.y, x1 = v1.x, y1 = v1.y;

                bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
                if (steep)
                {
                    swap(ref x0, ref y0);
                    swap(ref x1, ref y1);
                }

                bool xSwapped = (x0 > x1);
                if (xSwapped)
                {
                    swap(ref x0, ref x1);
                    swap(ref y0, ref y1);
                }

                var dx = x1 - x0;
                var dy = y1 - y0;
                var gradient = dy / dx;
                if (dx == 0.0)
                {
                    gradient = 1.0f;
                }

                // handle first endpoint
                var xend = Mathf.RoundToInt(x0);
                var yend = y0 + gradient * (xend - x0);
                var xgap = rfpart(x0 + 0.5f);
                var xpxl1 = xend; // this will be used in the main loop
                var ypxl1 = ipart(yend);
                if (steep)
                {
                    if (plot(ypxl1, xpxl1, rfpart(yend) * xgap, ref hitLoc_gs)) return true;
                    if (plot(ypxl1 + 1, xpxl1, fpart(yend) * xgap, ref hitLoc_gs)) return true;
                }
                else
                {
                    if (plot(xpxl1, ypxl1, rfpart(yend) * xgap, ref hitLoc_gs)) return true;
                    if (plot(xpxl1, ypxl1 + 1, fpart(yend) * xgap, ref hitLoc_gs)) return true;
                }
                var intery = yend + gradient; // first y-intersection for the main loop

                // handle second endpoint
                xend = round(x1);
                yend = y1 + gradient * (xend - x1);
                xgap = fpart(x1 + 0.5f);
                var xpxl2 = xend; //this will be used in the main loop
                var ypxl2 = ipart(yend);
                if (steep)
                {
                    if (plot(ypxl2, xpxl2, rfpart(yend) * xgap, ref hitLoc_gs)) return true;
                    if (plot(ypxl2 + 1, xpxl2, fpart(yend) * xgap, ref hitLoc_gs)) return true;
                }
                else
                {
                    if (plot(xpxl2, ypxl2, rfpart(yend) * xgap, ref hitLoc_gs)) return true;
                    if (plot(xpxl2, ypxl2 + 1, fpart(yend) * xgap, ref hitLoc_gs)) return true;
                }

                // main loop
                if (steep)
                {
                    for (int x = (xpxl1 + 1); x <= (xpxl2 - 1); x++)
                    {
                        if (plot(ipart(intery), x, rfpart(intery), ref hitLoc_gs)) return true;
                        if (plot(ipart(intery) + 1, x, fpart(intery), ref hitLoc_gs)) return true;
                        intery = intery + gradient;
                    }
                }
                else
                {
                    for (int x = (xpxl1 + 1); x <= (xpxl2 - 1); x++)
                    {
                        if (plot(x, ipart(intery), rfpart(intery), ref hitLoc_gs)) return true;
                        if (plot(x, ipart(intery) + 1, fpart(intery), ref hitLoc_gs)) return true;
                        intery = intery + gradient;
                    }
                }

                return false;
            }
    */
    public abstract class ConvolutionFilterBase<TF>
    {
        public abstract string FilterName
        {
            get;
        }


        public abstract TF Factor
        {
            get;
        }


        public abstract TF Bias
        {
            get;
        }


        public abstract TF[,] FilterMatrix
        {
            get;
        }

        public abstract TF AddMul(TF orig, TF x, TF y);
        public abstract TF Clamp(TF orig);

        protected float BaseClamp(float orig, float factor)
        {
            return orig * factor;
//            return Mathf.Clamp(orig * Factor, 0.0f, 1.0f);
        }
    }

    public class XXXFilter : ConvolutionFilterBase<float>
    {
        public override string FilterName
        {
            get { return "XXXFilter"; }
        }


        private float factor = 1.0f / 32.0f;
        public override float Factor
        {
            get { return factor; }
        }


        private float bias = 0.0f;
        public override float Bias
        {
            get { return bias; }
        }


        private float[,] filterMatrix =
            new float[,] { { 1, 1, 1, 1, 5, },
                       { 1, 1, 1, 1, 1, },
                       { 1, 1, 1, 1, 1, },
                        { 1, 5, 1, 1, 1, },
                       { 1, 1, 1, 1, 1, }, };


        public override float[,] FilterMatrix
        {
            get { return filterMatrix; }
        }

        public override float AddMul(float orig, float x, float y)
        {
            return orig + x * y;
        }
        public override float Clamp(float orig)
        {
            return BaseClamp(orig, Factor);
        }
    }

    public class Gaussian3x3BlurFilter : ConvolutionFilterBase<float>
    {
        public override string FilterName
        {
            get { return "Gaussian3x3BlurFilter"; }
        }


        private float factor = 1.0f / 16.0f;
        public override float Factor
        {
            get { return factor; }
        }


        private float bias = 0.0f;
        public override float Bias
        {
            get { return bias; }
        }


        private float[,] filterMatrix =
            new float[,] { { 1, 2, 1, },
                        { 2, 4, 2, },
                        { 1, 2, 1, }, };


        public override float[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
        public override float AddMul(float orig, float x, float y)
        {
            return orig + x * y;
        }
        public override float Clamp(float orig)
        {
            return BaseClamp(orig, Factor);
        }
    }

    public class Gaussian5x5BlurFilter : ConvolutionFilterBase<float>
    {
        public override string FilterName
        {
            get { return "Gaussian5x5BlurFilter"; }
        }


        private float factor = 1.0f / 159.0f;
        public override float Factor
        {
            get { return factor; }
        }


        private float bias = 0.0f;
        public override float Bias
        {
            get { return bias; }
        }


        private float[,] filterMatrix =
            new float[,] { { 2, 04, 05, 04, 2, },
                        { 4, 09, 12, 09, 4, },
                        { 5, 12, 15, 12, 5, },
                        { 4, 09, 12, 09, 4, },
                        { 2, 04, 05, 04, 2, }, };


        public override float[,] FilterMatrix
        {
            get { return filterMatrix; }
        }

        public override float AddMul(float orig, float x, float y)
        {
            return orig + x * y;
        }
        public override float Clamp(float orig)
        {
            return BaseClamp(orig, Factor);
        }
    }

    public static class ExtensionFilter
    {
        public static TF[,] ConvolutionFilter<TF, T>(this TF[,] pixelBuffer, T filter)
                                     where T : ConvolutionFilterBase<TF>
        {
            int width = pixelBuffer.GetLength(0);
            int height = pixelBuffer.GetLength(1);

            return pixelBuffer.ConvolutionFilter<TF, T>(0, 0, width, height, filter);
        }

        public static TF[,] ConvolutionFilter<TF, T>(this TF[,] pixelBuffer, int x0, int y0, int x1, int y1, T filter)
                                     where T : ConvolutionFilterBase<TF>
        {
            TF[,] resultBuffer = pixelBuffer.Clone() as TF[,];

            int width = pixelBuffer.GetLength(0);
            int height = pixelBuffer.GetLength(1);

            int filterWidth = filter.FilterMatrix.GetLength(1);
            int filterHeight = filter.FilterMatrix.GetLength(0);


            int filterOffset = (filterWidth - 1) / 2;


            var filterMatrix = filter.FilterMatrix;
            for (int offsetY = y0; offsetY < y1; offsetY++)
            {
                for (int offsetX = x0; offsetX < x1; offsetX++)
                {
                    var value = default(TF);

                    for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
                        {
                            int x = offsetX + filterX;
                            int y = offsetY + filterY;
                            if (x >= 0 && y >= 0 && x < width && y < height)
                            {
                                value = filter.AddMul(value, pixelBuffer[x, y], filterMatrix[filterY + filterOffset, filterX + filterOffset]);
                            }
                        }
                    }

                    resultBuffer[offsetX, offsetY] = filter.Clamp(value);
                }
            }

            return resultBuffer;
        }
    }

    [Serializable]
    public struct IntVector2 : IEquatable<IntVector2>
    {
        int m_X;
        int m_Y;

        public int X
        {
            get { return m_X; }
            set { m_X = value; }
        }
        public int Y
        {
            get { return m_Y; }
            set { m_Y = value; }
        }

        public static IntVector2 FromFloor(float x, float y)
        {
            return new IntVector2(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
        }
        public static IntVector2 FromRound(float x, float y)
        {
            return new IntVector2(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
        }
        public static IntVector2 FromRound(Vector3 v)
        {
            return FromRound(v.x, v.y);
        }

        public static implicit operator Vector3(IntVector2 d)
        {
            return new Vector3(d.X, d.Y);
        }

        public IntVector2(int x, int y)
        {
            m_X = x;
            m_Y = y;
        }

        public IntVector2(Vector3 v)
        {
            m_X = Mathf.FloorToInt(v.x);
            m_Y = Mathf.FloorToInt(v.y);
        }

        public void Reinit(int x, int y)
        {
            m_X = x;
            m_Y = y;
        }
        public override int GetHashCode()
        { 
            return m_X.GetHashCode() ^ m_Y.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj is IntVector2 ? Equals((IntVector2)obj) : false;
        }
        public bool Equals(IntVector2 obj)
        {
            return obj.m_X == this.m_X && obj.m_Y == this.m_Y;
        }
        public float Distance(IntVector2 other)
        {
            float dx = m_X - other.m_X;
            float dy = m_Y - other.m_Y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
        public float Distance(int x, int y)
        {
            float dx = m_X - x;
            float dy = m_Y - y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public int Manhattan
        {
            get
            {
                return Math.Abs(X) + Math.Abs(Y);
            }
        }
        public static IntVector2 operator -(IntVector2 a, IntVector2 b)
        {
            return new IntVector2(a.X - b.X, a.Y - b.Y);
        }
        public static IntVector2 operator +(IntVector2 a, IntVector2 b)
        {
            return new IntVector2(a.X + b.X, a.Y + b.Y);
        }
        public static bool operator ==(IntVector2 a, IntVector2 b)
        {
            return a.m_X == b.m_X && a.m_Y == b.m_Y;
        }
        public static bool operator !=(IntVector2 a, IntVector2 b)
        {
            return !(a == b);
        }

        public IntVector2 MakeDelta(int direction)
        {
            int dx, dy;
            Delta(direction, out dx, out dy);
            return new IntVector2(m_X + dx, m_Y + dy);
        }

        public static IntVector2 Up()
        {
            return new IntVector2(0, 1);
        }
        public static IntVector2 Down()
        {
            return new IntVector2(0, -1);
        }
        public static IntVector2 Right()
        {
            return new IntVector2(0, 1);
        }
        public static IntVector2 Left()
        {
            return new IntVector2(0, -1);
        }

        public const int NORTH = 0;
        public const int EAST = 1;
        public const int SOUTH = 2;
        public const int WEST = 3;

        public const int NORTHWEST = 4;
        public const int NORTHEAST = 5;
        public const int SOUTHWEST = 6;
        public const int SOUTHEAST = 7;

        public static int OppositeDir(int direction)
        {
            int result = -1;
            switch (direction)
            {
                case NORTH: result = SOUTH; break;
                case EAST: result = WEST; break;
                case SOUTH: result = NORTH; break;
                case WEST: result = EAST; break;

                case NORTHWEST: result = SOUTHEAST; break;
                case NORTHEAST: result = SOUTHWEST; break;
                case SOUTHWEST: result = NORTHEAST; break;
                case SOUTHEAST: result = NORTHWEST; break;
            }
            return result;
        }

        public static void Delta(int direction, out int dx, out int dy)
        {
            switch (direction)
            {
                case NORTH: dx = 0; dy = 1; break;
                case EAST: dx = 1; dy = 0; break;
                case SOUTH: dx = 0; dy = -1; break;
                case WEST: dx = -1; dy = 0; break;

                case NORTHWEST: dx = -1; dy = 1; break;
                case NORTHEAST: dx = 1; dy = 1; break;
                case SOUTHWEST: dx = -1; dy = -1; break;
                case SOUTHEAST: dx = 1; dy = -1; break;

                default: dx = 0; dy = 0; break;
            }
        }
    }

    public interface IZoneOutput
    {
        int GetGridVert(int x, int y, VoxelGenerator.GridVertDirection d, Vector3 value);
        void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color32 cola, Color32 colb, Color32 colc);
        void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 cola, Color32 colb, Color32 colc, Color32 cold);
        void AddFive(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e, Color32 cola, Color32 colb, Color32 colc, Color32 cold, Color32 cole);
        void AddPhysicsEdge(bool background, int a, int b);
    }

    public class VoxelGenerator : PolyGenerator
    {
        public class SinglePhysicsOnlyZoneOutput : IZoneOutput
        {
            public List<Vector3> m_Points = new List<Vector3>();
            public List<IntVector2> m_Edges = new List<IntVector2>();
            public SinglePhysicsOnlyZoneOutput()
            {
            }

            public void AddPhysicsEdge(bool background, int a, int b)
            {
                m_Edges.Add(new IntVector2(a, b));
            }

            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color32 cola, Color32 colb, Color32 colc)
            {
            }
            public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 cola, Color32 colb, Color32 colc, Color32 cold)
            {
            }
            public void AddFive(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e, Color32 cola, Color32 colb, Color32 colc, Color32 cold, Color32 cole)
            {
            }

            public int GetGridVert(int x, int y, VoxelGenerator.GridVertDirection d, Vector3 value)
            {
                m_Points.Add(value);
                return m_Points.Count - 1;
            }
        }

        public class VoxelGeneratorZoneOutput : IZoneOutput
        {
            VoxelGenerator m_Gen;
            public VoxelGeneratorZoneOutput(VoxelGenerator gen)
            {
                m_Gen = gen;
            }
            public void AddPhysicsEdge(bool background, int a, int b)
            {
                m_Gen.AddPhysicsEdge(background, a, b);

                // @TEST Occluding edge
                /*
                var av = m_Gen.m_VertList[a];
                var bv = m_Gen.m_VertList[b];

                float v = 50.0f;
                m_Gen.AddQuad(bv + v*Vector3.back, av + v * Vector3.back, av + v * Vector3.forward, bv + v * Vector3.forward, new Color32(255,255,255,255));
                */
            }

            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color32 cola, Color32 colb, Color32 colc)
            {
                m_Gen.AddTriangle(a, b, c, cola, colb, colc);
            }

            public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 cola, Color32 colb, Color32 colc, Color32 cold)
            {
                m_Gen.AddQuad(a, b, c, d, cola, colb, colc, cold);
            }

            public void AddFive(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e, Color32 cola, Color32 colb, Color32 colc, Color32 cold, Color32 cole)
            {
                m_Gen.AddFive(a, b, c, d, e, cola, colb, colc, cold, cole);
            }

            public int GetGridVert(int x, int y, VoxelGenerator.GridVertDirection d, Vector3 value)
            {
                return m_Gen.GetGridVert(x, y, d, value);
            }
        }


        public class Layer
        {
            VoxelGenerator m_Gen;
            public float[,] m_Values;

            IntVector2 m_Dimension;
            Bounds m_LayerBounds;

            public byte[,] m_DebugMaterials;
            public static Color32[] m_DebugMaterialColor;

            static Layer()
            {
                m_DebugMaterialColor = new Color32[256];
                for(int i=0;i<m_DebugMaterialColor.Length;i++)
                {
                    m_DebugMaterialColor[i] = new Color32((byte)UnityEngine.Random.Range(0, 255), (byte)UnityEngine.Random.Range(0, 255), (byte)UnityEngine.Random.Range(0, 255), 255);
                }
            }

            public float GetValue(int x, int y)
            {
                if (x < 0 || y < 0) return ZERO_CROSSING_VALUE - 0.5f;
                if (x >= m_Dimension.X || y >= m_Dimension.Y) return ZERO_CROSSING_VALUE - 0.5f;
                return m_Values[x, y];
            }

            public Color32 MaterialColor(IntVector2 zone, int setMask)
            {
                if (zone.X < 0 || zone.Y < 0) return new Color32(0, 0, 0, 255);
                if (zone.X >= m_Dimension.X || zone.Y >= m_Dimension.Y) return new Color32(0, 0, 0, 255);
                int index = (int)m_DebugMaterials[zone.X, zone.Y];
                if (index > m_DebugMaterialColor.Length || index < 0)
                {
                    Debug.Log("Bad MaterialColor???");
                    return new Color32();
                }
                return m_DebugMaterialColor[index];

                if (DigPower > 1.0f) return new Color32(0, 0, 0, 255);
                int count = (setMask & 1) + ((setMask & 2) >> 1) + ((setMask & 4) >> 2) + ((setMask & 8) >> 3);
                if (count == 4) return new Color32(255, 0, 0, 255);
                if (count == 1) return new Color32(0, 0, 255, 255);
                if (count == 2) return new Color32(0, 255, 0, 255);
                return new Color32(255, 0, 255, 255);
            }

            public float GetValue(IntVector2 p)
            {
                return GetValue(p.X, p.Y);
            }

            // Zone, and then 0-1 location in the zone
            public float GetValueAtZone(IntVector2 zone, Vector2 pos)
            {
                float bl = GetValue(zone);
                float br = GetValue(new IntVector2(zone.X + 1, zone.Y));
                float ul = GetValue(new IntVector2(zone.X, zone.Y + 1));
                float ur = GetValue(new IntVector2(zone.X + 1, zone.Y + 1));

                return VoxelGenerator.Bilerp(bl, br, ul, ur, pos);
            }

            public bool IsZoneClear(IntVector2 zone)
            {
                float bl = GetValue(zone);
                float br = GetValue(new IntVector2(zone.X + 1, zone.Y));
                float ul = GetValue(new IntVector2(zone.X, zone.Y + 1));
                float ur = GetValue(new IntVector2(zone.X + 1, zone.Y + 1));

                return (bl < ZERO_CROSSING_VALUE) &&
                    (br < ZERO_CROSSING_VALUE) &&
                    (ul < ZERO_CROSSING_VALUE) &&
                    (ur < ZERO_CROSSING_VALUE);
            }

            public bool IsBoundingBoxClear(Vector3 center_gs, Vector3 up_gs, float halfWidth, float halfHeight)
            {
                Vector3 right_gs = Vector3.Cross(Vector3.forward, up_gs);
                Vector3 bottomLeft_gs = center_gs - right_gs * halfWidth - up_gs * halfHeight;
                for(float y=0;y<halfHeight*2.0f;y+=1.0f)
                {
                    for(float x=0;x<halfWidth*2.0f;x+=1.0f)
                    {
                        Vector3 pos_gs = bottomLeft_gs + right_gs * x + up_gs * y;
                        Vector3 pos_ws = m_Gen.GridSpaceToWorld(pos_gs);
                        var result = IsZoneClear(IntVector2.FromFloor(pos_gs.x, pos_gs.y));
                        Debug.DrawLine(pos_ws + 10.0f * Vector3.back, pos_ws + 10.0f * Vector3.forward, result ? Color.cyan : Color.red);
                        if (!result)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            // Get rid of single positive values surrounded by negative
            public void CleanScraps(IntVector2 vi_ll_gs, IntVector2 vi_ur_gs)
            {
                for (int x = vi_ll_gs.X; x <= vi_ur_gs.X; x++)
                {
                    for (int y = vi_ll_gs.Y; y <= vi_ur_gs.Y; y++)
                    {
                       if (GetValue(x - 1,y) < ZERO_CROSSING_VALUE &&
                            GetValue(x + 1, y) < ZERO_CROSSING_VALUE &&
                            GetValue(x, y-1) < ZERO_CROSSING_VALUE &&
                            GetValue(x, y+1) < ZERO_CROSSING_VALUE )
                        {
                            SetValue(x, y, ZERO_CROSSING_VALUE - 0.5f);
                        }
                    }
                }
            }

            public bool SetValue(int x, int y, float v)
            {
                if (x < 0 || y < 0) return false;
                if (x >= m_Dimension.X || y >= m_Dimension.Y) return false;
                var oldVal = m_Values[x, y];
                float d = oldVal - v;
                if (d * d < 0.01f) return true;

                m_Values[x, y] = v;

                for (int dx = -1; dx <= 0; dx++)
                    for (int dy = -1; dy <= 0; dy++)
                        m_Gen.ZoneNotify(x + dx, y + dy);

                return true;
            }

            public bool IntersectCell(int x, int y, Vector3 start, Vector3 end, ref IntersectInfo info)
            {
                SinglePhysicsOnlyZoneOutput zo = new SinglePhysicsOnlyZoneOutput();
                int mask = m_Gen.GenerateQuadForZone(this, new IntVector2(x, y), true, zo);
                bool hits = false;

                //Debug.DrawLine(m_Gen.transform.TransformPoint(start), m_Gen.transform.TransformPoint(end), Color.magenta, 0.0f, false);

                foreach (var edge in zo.m_Edges)
                {
                    // Convert edge to grid space
                    var starte = new Vector3(zo.m_Points[edge.X].x * m_Dimension.X, zo.m_Points[edge.X].y * m_Dimension.Y);
                    var ende = new Vector3(zo.m_Points[edge.Y].x * m_Dimension.Y, zo.m_Points[edge.Y].y * m_Dimension.Y);

                    Vector3 sect1 = Vector3.zero;
                    var sect2 = Vector3.zero;
                    hits = TwoDee.Math3d.LineLineIntersection(ref sect1, starte, ende - starte, start, end - start);

                    Debug.DrawLine(m_Gen.transform.TransformPoint((1.0f / m_Dimension.X) * sect1),
                        m_Gen.transform.TransformPoint((1.0f / m_Dimension.X) * sect2), Color.blue, 0.0f, false);

                    Debug.DrawLine(m_Gen.transform.TransformPoint(zo.m_Points[edge.X]), m_Gen.transform.TransformPoint(zo.m_Points[edge.Y]), Color.magenta, 0.0f, false);
                    if (hits)
                    {
                        float deltaDist = (sect1 - starte).magnitude;
                        if (deltaDist > 2.0f)
                        {
                            UnityEngine.Debug.Log("Bad line intersection");
                            hits = TwoDee.Math3d.LineLineIntersection(ref sect1, starte, ende - starte, start, end - start);
                        }
                        info = new IntersectInfo()
                        {
                            m_Pos_ws = m_Gen.transform.TransformPoint((1.0f / m_Dimension.X) * sect1),
                            m_Normal_ws = m_Gen.transform.TransformDirection(Vector3.Cross(Vector3.forward, zo.m_Points[edge.Y] - zo.m_Points[edge.X]).normalized),
                            m_Gx = x,
                            m_Gy = y
                        };
                        return true;
                    }
                }

                var v00 = m_Gen.transform.TransformPoint(new Vector3(x / (1.0f * m_Dimension.X), y / (1.0f * m_Dimension.Y)));
                var v01 = m_Gen.transform.TransformPoint(new Vector3(x / (1.0f * m_Dimension.X), (y + 1) / (1.0f * m_Dimension.Y)));
                var v10 = m_Gen.transform.TransformPoint(new Vector3((x + 1) / (1.0f * m_Dimension.X), y / (1.0f * m_Dimension.Y)));
                var v11 = m_Gen.transform.TransformPoint(new Vector3((x + 1) / (1.0f * m_Dimension.X), (y + 1) / (1.0f * m_Dimension.Y)));
                Debug.DrawLine(v00, v01, hits ? Color.black : Color.cyan);
                Debug.DrawLine(v00, v10, hits ? Color.black : Color.cyan);
                Debug.DrawLine(v11, v01, hits ? Color.black : Color.cyan);
                Debug.DrawLine(v11, v10, hits ? Color.black : Color.cyan);

                return false;
            }

            public bool IntersectSegmentCheap(Vector3 v0, Vector3 v1, out IntersectInfo info)
            {
                info = new IntersectInfo();
                var delta = (v1 - v0);
                var deltaNrm = delta.normalized;
                Vector3 v = v0;
                Vector3 vprev = v;
                float distance = delta.magnitude;

                IntVector2 iv = IntVector2.FromRound(v0);
                IntVector2 ivprev = iv;
                for (float d=0.0f;d<=distance;d+=0.5f)
                {
                    v.x = deltaNrm.x * d + v0.x;
                    v.y = deltaNrm.y * d + v0.y;
                    int x = Mathf.RoundToInt(v.x);
                    int y = Mathf.RoundToInt(v.y);
                    iv.Reinit(x, y); 
                    if (GetValue(iv) > ZERO_CROSSING_VALUE)
                    {
                        info.m_Gx = ivprev.X;
                        info.m_Gy = ivprev.Y;
                        info.m_Pos_ws = m_Gen.GridSpaceToWorld(vprev);
                        info.m_Normal_ws = -deltaNrm;

                        return true;
                    }
                    ivprev = iv;
                    vprev = v;
                }

                info = null;
                return false;
            }

            public bool IntersectSegment(Vector3 v0, Vector3 v1, out IntersectInfo info, bool cheap)
            {
                if(cheap)
                {
                    return IntersectSegmentCheap(v0, v1, out info);
                }
                info = null;

                // Similar to the XiaoLin Wu Algorithm except we care about order of plotting pixels.
                var delta = (v1 - v0);
                float endT = (v1 - v0).magnitude;
                float slope = (delta.x == 0.0f) ? 2.0f : (delta.y / delta.x); // If we can't divide by zero just put a number that the slope the other direction will always win
                float invSlope = (delta.y == 0.0f) ? 2.0f : (delta.x / delta.y);

                float xDir = Mathf.Sign(delta.x);
                float yDir = Mathf.Sign(delta.y);
                Vector3 bigIncrement;
                Vector3 smallIncrement;

                bool steep = Mathf.Abs(slope) > Mathf.Abs(invSlope);
                if (steep)
                {
                    bigIncrement = new Vector3(0.0f, yDir);
                    smallIncrement = new Vector3(yDir * invSlope, 0.0f);
                }
                else
                {
                    // Not steep- we are going along X and slowly incrementing Y.
                    bigIncrement = new Vector3(xDir, 0.0f);
                    smallIncrement = new Vector3(0.0f, xDir * slope);
                }
                float tPerIncrement = (bigIncrement + smallIncrement).magnitude;

                int iterations = Mathf.FloorToInt(endT / tPerIncrement);

                Vector3 currentLoc = v0;
                for (int i = 0; i <= iterations; i++)
                {
                    Vector3 lastLoc = currentLoc;
                    Vector3 nextLoc = (currentLoc + bigIncrement + smallIncrement);
                    // Now the trick is when we increment it's possible to skip over exactly 1 adjacent cell- we know though which we will hit first by finding the t of each one.
                    Vector3 distancesToNextCell = new Vector3(currentLoc.x % 1.0f, currentLoc.y % 1.0f);
                    if (xDir > 0.0f) distancesToNextCell.x = 1.0f - distancesToNextCell.x; // If we're going the other way we need the distance to the other side
                    if (yDir > 0.0f) distancesToNextCell.y = 1.0f - distancesToNextCell.y; // If we're going the other way we need the distance to the other side

                    float tSmall = steep ? distancesToNextCell.x / smallIncrement.x : distancesToNextCell.y / smallIncrement.y;
                    float tBig = steep ? distancesToNextCell.y : distancesToNextCell.x;

                    Vector3 firstInc, secondInc;
                    // Do the smaller once first since that's the time we will hit first.
                    if (tSmall < tBig)
                    {
                        firstInc = smallIncrement;
                        secondInc = bigIncrement;
                    }
                    else
                    {
                        firstInc = bigIncrement;
                        secondInc = smallIncrement;
                    }

                    currentLoc += firstInc;
                    var xCell0 = Mathf.FloorToInt(currentLoc.x);
                    var yCell0 = Mathf.FloorToInt(currentLoc.y);
                    if (IntersectCell(xCell0, yCell0, lastLoc, nextLoc, ref info)) return true;
                    // Check if we hit a different cell (note we obviously always do if we do big increment second)
                    currentLoc += secondInc;
                    var xCell = Mathf.FloorToInt(currentLoc.x);
                    var yCell = Mathf.FloorToInt(currentLoc.y);
                    if (xCell0 != xCell || yCell0 != yCell)
                    {
                        if (IntersectCell(xCell, yCell, lastLoc, nextLoc, ref info)) return true;
                    }

                }

                return false;
            }

            public void Resize(int ix, int iy)
            {
                m_Dimension = new IntVector2(ix, iy);
                m_Values = new float[ix, iy];
                {
                    for (int y = 0; y < m_Values.GetLength(1); y++)
                        for (int x = 0; x < m_Values.GetLength(0); x++)
                            m_Values[x, y] = ZERO_CROSSING_VALUE - 1.0f;
                }

                m_DebugMaterials = new byte[ix, iy];
            }

            public Layer(VoxelGenerator gen)
            {
                m_Gen = gen;
                Resize(gen.m_DimensionX, gen.m_DimensionY);
            }

            public float DigPower
            {
                set; get;
            }
            public float Z
            {
                set; get;
            }
            public bool Background
            {
                set; get;
            }
        }

        public class IntersectInfo
        {
            public Vector3 m_Pos_ws;
            public Vector3 m_Normal_ws;
            public int m_Gx;
            public int m_Gy;
        }

        public bool CheapIntersectSegment(Vector3 start_ws, Vector3 end_ws, out IntersectInfo info)
        {
            return IntersectSegment(start_ws, end_ws, out info, true);
        }

        public bool IntersectSegment(Vector3 start_ws, Vector3 end_ws, out IntersectInfo info, bool cheap=false)
        {
            info = null;

            // Find out where it intersects with our bounds if all.
            Bounds localBounds = new Bounds(new Vector3(0.5f, 0.5f), new Vector3(1.0f, 1.0f));

            var start_ls = transform.InverseTransformPoint(start_ws);
            var end_ls = transform.InverseTransformPoint(end_ws);
            Vector3 hitloc_ls = start_ls;
            if (localBounds.Contains(start_ls) || localBounds.IntersectLineSegment(start_ls, end_ls, out hitloc_ls))
            {
                Vector3 startLoc_gs = Vector3.Scale(hitloc_ls, new Vector3(m_DimensionX, m_DimensionY));
                Vector3 end_gs = Vector3.Scale(end_ls, new Vector3(m_DimensionX, m_DimensionY));

                var results = new List<IntersectInfo>();
                foreach (var layer in m_Layers)
                {
                    if (layer.Background) continue;
                    bool hit = layer.IntersectSegment(startLoc_gs, end_gs, out info, cheap);
                    if (hit)
                    {
                        results.Add(info);
                    }
                }

                if (results.Count > 0)
                {
                    results.Sort((a, b) =>
                    (a.m_Pos_ws - start_ws).sqrMagnitude.CompareTo((b.m_Pos_ws - start_ws).sqrMagnitude)
                    );
                    info = results[0];
                    return true;
                }
            }

            return false;
        }

        public VoxelGeneratorMiniMap MiniMap
        {
            get
            {
                if (m_MiniMap == null)
                {
                    m_MiniMap = new VoxelGeneratorMiniMap(this);
                }
                return m_MiniMap;
            }
        }
        VoxelGeneratorMiniMap m_MiniMap;

        public Layer GetLayer(int layer)
        {
            return m_Layers[layer];
        }
        public int GetLayerCount()
        {
            return m_Layers.Count;
        }

        protected List<Layer> m_Layers = new List<Layer>();
        VoxelGeneratorZoneOutput m_ZoneOutput;

        public const int ZONES_PER_BLOCK_DIM = 10; // a block uses this many zones

        //        IntVector m_Dimension = new IntVector(63, 64);
        IntVector2 m_Dimension = new IntVector2(88, 88);
        public IntVector2 Dimension
        {
            get { return m_Dimension; }
        }
        public int m_DimensionX = 88;
        public int m_DimensionY = 88;

        public float Radius
        {
            get { return m_DimensionX / 2.0f; }
        }

        [HideInInspector][NonSerialized]
        //public int m_ClipX0 = -555;
        public int m_ClipX0 = -300;
        [HideInInspector][NonSerialized]
        //public int m_ClipX1 = 555;
        public int m_ClipX1 = 300;
        [HideInInspector]
        [NonSerialized]
        //public int m_ClipY0 = -999;//150;//300;
        public int m_ClipY0 = 150;//300;
        [HideInInspector]
        [NonSerialized]
        public int m_ClipY1 = 9999;

        public bool m_RoundWorld = false;

        public float ZoneWidth { get { return m_ZoneWidth; } }
        float m_ZoneWidth;
        public float ZoneHeight { get { return m_ZoneHeight; } } 
        float m_ZoneHeight;

        bool[,] m_BlocksClean;
        int m_BlocksY;
        int m_BlocksX;

        int[,,] m_GridIndices;
        List<Vector3> m_VertList = new List<Vector3>();
        List<int> m_PhysicsEdges = new List<int>();

        void AddPhysicsEdge(bool background, int a, int b)
        {
            if (background) return;
            m_PhysicsEdges.Add(a);
            m_PhysicsEdges.Add(b);
        }

        void AddPhysics(int blockId)
        {
            var go = GetComponent<PolyGenerator>().GetMeshObject(blockId);
            var mc = go.GetComponent<MeshCollider>();

            Mesh mesh = new Mesh();
            Vector3[] meshVerts = new Vector3[m_VertList.Count * 2];
            float zAmount = 10.0f;
            Vector3 zPlus = new Vector3(0.0f, 0.0f, zAmount);
            for (int i = 0; i < m_VertList.Count; i++)
            {
                meshVerts[i * 2 + 0] = m_VertList[i] + zPlus;
                meshVerts[i * 2 + 1] = m_VertList[i] - zPlus;
            }
            List<int> meshTriangleList = new List<int>();

            for (int i = 0; i < m_PhysicsEdges.Count / 2; i++)
            {
                int index1 = m_PhysicsEdges[i * 2 + 0];
                int index2 = m_PhysicsEdges[i * 2 + 1];
                meshTriangleList.Add(index1 * 2);
                meshTriangleList.Add(index2 * 2);
                meshTriangleList.Add(index2 * 2 + 1);

                meshTriangleList.Add(index1 * 2);
                meshTriangleList.Add(index2 * 2 + 1);
                meshTriangleList.Add(index1 * 2 + 1);

                // @TEST - Show collision normals
                /*
                var cross = Vector3.Cross(meshVerts[index2 * 2] - meshVerts[index1 * 2], meshVerts[index2 * 2 + 1] - meshVerts[index1 * 2]);
                var debugDrawPoint = Vector3.Lerp(transform.TransformPoint(meshVerts[index1 * 2]),
                    transform.TransformPoint(meshVerts[index2 * 2]), 0.5f);  debugDrawPoint.z = -0.1f;
                Debug.DrawLine(debugDrawPoint, debugDrawPoint + cross, Color.black, 1000);
                cross = Vector3.Cross(meshVerts[index2 * 2 + 1] - meshVerts[index1 * 2], meshVerts[index1 * 2 + 1] - meshVerts[index1 * 2]);
                //debugDrawPoint = transform.TransformPoint(meshVerts[index2 * 2]); debugDrawPoint.z = 0.0f;
                Debug.DrawLine(debugDrawPoint, debugDrawPoint + cross, Color.black, 1000);
                */
            }

            mesh.vertices = meshVerts;
            mesh.triangles = meshTriangleList.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            mc.sharedMesh = mesh;
        }

        public enum GridVertDirection
        {
            Left,
            Right,
            Up,
            Down
        };

        
        // Get the index in the vert list at the given Vector3 point if it already exists.  If not, create it.
        public int GetGridVert(int x, int y, GridVertDirection d, Vector3 value)
        {
            int typeDim = 0;
            switch (d)
            {
                case GridVertDirection.Up:
                    y++;
                    typeDim = 1;
                    break;
                case GridVertDirection.Down:
                    typeDim = 1;
                    break;
                case GridVertDirection.Right:
                    x++;
                    typeDim = 0;
                    break;
                case GridVertDirection.Left:
                    typeDim = 0;
                    break;
            }
            if (m_GridIndices[x, y, typeDim] == -1)
            {
                m_GridIndices[x, y, typeDim] = m_VertList.Count;
                m_VertList.Add(value);
            }
            else
            {
                var vec = m_VertList[m_GridIndices[x, y, typeDim]];
                if (vec != value)
                {
                    Debug.Log("Values differ??\n");
                }
            }
            return m_GridIndices[x, y, typeDim];
        }




        [Serializable]
        public class VoxelGeneratorProxyLayer
        {
            Rle2DArrayProxy m_ValueData;
            Rle2DArrayProxy m_DebugMaterialData;

            public void AppendConsecutive(StringBuilder builder, int consecutiveValue, int numConsecutiveValues)
            {
                if (numConsecutiveValues == 0) return;
                builder.AppendFormat("{0}_{1}\n", consecutiveValue, numConsecutiveValues);
            }

            public void Save(VoxelGenerator input, int layer)
            {
                m_ValueData = new Rle2DArrayProxy();
                m_ValueData.Save(input.m_Layers[layer].m_Values);
                m_DebugMaterialData = new Rle2DArrayProxy();
                m_DebugMaterialData.Save(input.m_Layers[layer].m_DebugMaterials);
            }

            public void Load(VoxelGenerator output, int layer)
            {
                output.m_Layers[layer].m_Values = m_ValueData.LoadFloat();
                output.m_Layers[layer].m_DebugMaterials = m_DebugMaterialData.LoadByte();
            }
        }

        [Serializable]
        public class VoxelGeneratorProxy
        {
            public int m_X;
            public int m_Y;
            public List<VoxelGeneratorProxyLayer> m_Layers;

            // Lighting data, may move to separate structure
            Rle2DArrayProxy m_LightGrid = new Rle2DArrayProxy();

            public VoxelGeneratorProxy()
            {
            }

            public void Save(VoxelGenerator input)
            {
                m_Layers = new List<VoxelGeneratorProxyLayer>();

                m_X = input.m_Dimension.X;
                m_Y = input.m_Dimension.Y;

                for (int i = 0; i < input.m_Layers.Count; i++)
                {
                    m_Layers.Add(new VoxelGeneratorProxyLayer());
                    m_Layers[i].Save(input, i);
                }

                m_LightGrid.Save(input.LightingDataInstance.m_LitValues);
            }

            public void Load(VoxelGenerator output)
            {
                output.Resize(m_X, m_Y);

                for (int i = 0; i < m_Layers.Count; i++)
                {
                    m_Layers[i].Load(output, i);
                }

                output.LightingDataInstance.m_LitValues = m_LightGrid.LoadByte();
            }
        }

        void Resize(int x, int y)
        {
            m_Dimension = new IntVector2(x, y);
            transform.position = new Vector3(m_Dimension.X * -0.5f, m_Dimension.Y * -0.5f);
            transform.localScale = new Vector3(m_Dimension.X * 1.0f, m_Dimension.Y * 1.0f, 1.0f);

            m_LightingData.Resize(x, y);

            foreach (var layer in m_Layers)
            {
                layer.Resize(m_Dimension.X, m_Dimension.Y);
            }
            // These indices are the corners of the zones, so we need an extra set in each dim.
            m_GridIndices = new int[m_Dimension.X + 1, m_Dimension.Y + 1, 2];
            for (int i = 0; i < m_GridIndices.GetLength(0); i++)
                for (int j = 0; j < m_GridIndices.GetLength(1); j++)
                    for (int k = 0; k < m_GridIndices.GetLength(2); k++)
                        m_GridIndices[i, j, k] = -1;

            m_ZoneWidth = 1.0f / m_Dimension.X;
            m_ZoneHeight = 1.0f / m_Dimension.Y;

            // Everything is dirty now
            m_BlocksY = (m_Dimension.Y + ZONES_PER_BLOCK_DIM - 1) / ZONES_PER_BLOCK_DIM;
            m_BlocksX = (m_Dimension.X + ZONES_PER_BLOCK_DIM - 1) / ZONES_PER_BLOCK_DIM;
            m_BlocksClean = new bool[m_BlocksX, m_BlocksY];
        }

        public void ResetBlocksClean()
        {
            m_BlocksClean = new bool[m_BlocksX, m_BlocksY];
        }


        public VoxelGenerator()
        {
            m_ZoneOutput = new VoxelGeneratorZoneOutput(this);
        }

        Vector3 WorldSpaceVectorToGrid(Vector3 v_ws)
        {
            Vector3 v_ls = transform.InverseTransformVector(v_ws);
            return new Vector3(v_ls.x * m_Dimension.X, v_ls.y * m_Dimension.Y);
        }
        public Vector3 WorldSpaceToGrid(Vector3 v_ws)
        {
            Vector3 v_ls = transform.InverseTransformPoint(v_ws);
            return new Vector3(v_ls.x * m_Dimension.X, v_ls.y * m_Dimension.Y);
        }
        public Vector3 GridSpaceToWorld(Vector3 v_gs)
        {
            return transform.TransformPoint(new Vector3(v_gs.x / m_Dimension.X, v_gs.y / m_Dimension.Y));
        }

        public Vector3 StartingPoint
        {
            set; get;
        }

        public delegate void ZoneNotifyFunc(int x, int y);
        public ZoneNotifyFunc m_ZoneNotifies;
        void ZoneNotify(int x, int y)
        {
            if (m_ZoneNotifies != null)
            {
                m_ZoneNotifies(x, y);
            }
        }

        public static float Bilerp(float bl, float br, float ul, float ur, Vector2 pos)
        {
            float lerpTop = Mathf.Lerp(ul, ur, pos.x);
            float lerpBot = Mathf.Lerp(bl, br, pos.x);

            return Mathf.Lerp(lerpBot, lerpTop, pos.y);
        }


        // Voxel cases
        public const int FILLLED = (1 | 2 | 4 | 8);

        // single corner true cases
        public const int CORNER_BOTTOM_LEFT = 1;
        public const int CORNER_BOTTOM_RIGHT = 2;
        public const int CORNER_TOP_LEFT = 4;
        public const int CORNER_TOP_RIGHT = 8;

        // Side cases:
        public const int SIDE_BOTTOM = (1 | 2);
        public const int SIDE_LEFT = (1 | 4);
        public const int SIDE_RIGHT = (2 | 8);
        public const int SIDE_TOP = (4 | 8);

        // single corner not filled
        public const int CHIPPED_TOP_RIGHT = (1 | 2 | 4);
        public const int CHIPPED_BOTTOM_RIGHT = (1 | 4 | 8);
        public const int CHIPPED_TOP_LEFT = (1 | 2 | 8);
        public const int CHIPPED_BOTTOM_LEFT = (2 | 4 | 8);

        // Two triangle cases
        public const int CORNERS_BOTTOM_LEFT_AND_TOP_RIGHT = (1 | 8);
        public const int CORNERS_BOTTOM_RIGHT_AND_TOP_LEFT = (2 | 4);

        public const float ZERO_CROSSING_VALUE = 0.0f;

        // create polygons for the zero-crossing
        public int GenerateQuadForZone(Layer layer, IntVector2 zone, bool black, IZoneOutput zoneOutput)
        {
            var z = layer.Z;
            var bg = layer.Background;
            float w = m_ZoneWidth;
            float h = m_ZoneHeight;
            float xw = zone.X * m_ZoneWidth;
            float yh = zone.Y * m_ZoneHeight;
            float xw1 = (zone.X+1) * m_ZoneWidth;
            float yh1 = (zone.Y+1) * m_ZoneHeight;

            var color = new Color32(255, 0, 0, 255);
            var color2 = new Color32(0, 255, 0, 255);
            var color3 = new Color32(0, 0, 255, 255);
            var color4 = new Color32(255, 0, 255, 255);
            var color5 = new Color32(255, 255, 255, 255);
            if (black)
            {
                color = new Color32(0, 0, 0, 255);
                color2 = new Color32(0, 0, 0, 255);
                color3 = new Color32(0, 0, 0, 255);
                color4 = new Color32(0, 0, 0, 255);
                color5 = new Color32(0, 0, 0, 255);
            }

            float bl = layer.GetValue(zone);
            float br = layer.GetValue(new IntVector2(zone.X + 1, zone.Y));
            float tl = layer.GetValue(new IntVector2(zone.X, zone.Y + 1));
            float tr = layer.GetValue(new IntVector2(zone.X + 1, zone.Y + 1));

            float lt = Mathf.InverseLerp(bl, tl, ZERO_CROSSING_VALUE);
            Vector3 x0yt_left = new Vector3(xw, yh + lt * h, z);
            int leftIndex = zoneOutput.GetGridVert(zone.X, zone.Y, GridVertDirection.Left, x0yt_left);

            float rt = Mathf.InverseLerp(br, tr, ZERO_CROSSING_VALUE);
            Vector3 x1yt_right = new Vector3(xw1, yh + rt * h, z);
            int rightIndex = zoneOutput.GetGridVert(zone.X, zone.Y, GridVertDirection.Right, x1yt_right);

            float bt = Mathf.InverseLerp(bl, br, ZERO_CROSSING_VALUE);
            Vector3 xty0_bottom = new Vector3(xw + bt * w, yh, z);
            int bottomIndex = zoneOutput.GetGridVert(zone.X, zone.Y, GridVertDirection.Down, xty0_bottom);

            float ut = Mathf.InverseLerp(tl, tr, ZERO_CROSSING_VALUE);
            Vector3 xty1_top = new Vector3(xw + ut * w, yh1, z);
            int topIndex = zoneOutput.GetGridVert(zone.X, zone.Y, GridVertDirection.Up, xty1_top);

            Vector3 x0y0 = new Vector3(xw, yh, z);
            Vector3 x1y0 = new Vector3(xw1, yh, z);
            Vector3 x0y1 = new Vector3(xw, yh1, z);
            Vector3 x1y1 = new Vector3(xw1, yh1, z);

            // Set of verts that are above zero
            int setMask = ((bl >= ZERO_CROSSING_VALUE) ? 1 : 0) |
                ((br >= ZERO_CROSSING_VALUE) ? 2 : 0) |
                ((tl >= ZERO_CROSSING_VALUE) ? 4 : 0) |
                ((tr >= ZERO_CROSSING_VALUE) ? 8 : 0);

            var blcolor = layer.MaterialColor(zone, setMask);
            var brcolor = layer.MaterialColor(new IntVector2(zone.X + 1, zone.Y), setMask);
            var tlcolor = layer.MaterialColor(new IntVector2(zone.X, zone.Y + 1), setMask);
            var trcolor = layer.MaterialColor(new IntVector2(zone.X + 1, zone.Y + 1), setMask);

            float t;
            switch (setMask)
            {
                // full case
                case FILLLED:
 //                   if ( (tlm == trm && blm == brm) ||
 //                      (trm == brm && tlm == blm))
                    {
                        zoneOutput.AddTriangle(x0y0, x1y1, x1y0, blcolor, trcolor, brcolor);
                        zoneOutput.AddTriangle(x0y0, x0y1, x1y1, blcolor, tlcolor, trcolor);
                    }
                    /* @TEST different material color interp
                    else
                    {
                        Vector3 xmym = new Vector2(xw + w/2, yh + h/2);
                        Color32 midColor = new Color32((byte)((blcolor.r + brcolor.r + tlcolor.r + trcolor.r) / 4),
                           (byte)((blcolor.g + brcolor.g + tlcolor.g + trcolor.g) / 4),
                        (byte)((blcolor.b + brcolor.b + tlcolor.b + trcolor.b) / 4),
                       (byte)((blcolor.a + brcolor.a + tlcolor.a + trcolor.a) / 4));
                        zoneOutput.AddTriangle(x0y0, xmym, x1y0, blcolor, midColor, brcolor);
                        zoneOutput.AddTriangle(x0y0, x0y1, xmym, blcolor, tlcolor, midColor);
                        zoneOutput.AddTriangle(xmym, x1y1, x1y0, midColor, trcolor, brcolor);
                        zoneOutput.AddTriangle(xmym, x0y1, x1y1, midColor, tlcolor, trcolor);
                    }
                    */
                    break;
                // single corner true cases
                case CORNER_BOTTOM_LEFT:
                    zoneOutput.AddTriangle(x0y0, x0yt_left, xty0_bottom, blcolor, blcolor, blcolor);
                    zoneOutput.AddPhysicsEdge(bg, leftIndex, bottomIndex);
                    break;
                case CORNER_BOTTOM_RIGHT:
                    zoneOutput.AddTriangle(x1y0, xty0_bottom, x1yt_right, brcolor, brcolor, brcolor);
                    zoneOutput.AddPhysicsEdge(bg, bottomIndex, rightIndex);
                    break;
                case CORNER_TOP_LEFT:
                    zoneOutput.AddTriangle(x0y1, xty1_top, x0yt_left, tlcolor, tlcolor, tlcolor);
                    zoneOutput.AddPhysicsEdge(bg, topIndex, leftIndex);
                    break;
                case CORNER_TOP_RIGHT:
                    zoneOutput.AddTriangle(x1y1, x1yt_right, xty1_top, trcolor, trcolor, trcolor);
                    zoneOutput.AddPhysicsEdge(bg, rightIndex, topIndex);
                    break;
                // Side cases:
                case SIDE_BOTTOM:
                    zoneOutput.AddQuad(x0y0, x0yt_left, x1yt_right, x1y0, blcolor, blcolor, brcolor, brcolor);
                    zoneOutput.AddPhysicsEdge(bg, leftIndex, rightIndex);
                    break;
                case SIDE_LEFT:
                    zoneOutput.AddQuad(x0y0, x0y1, xty1_top, xty0_bottom, blcolor, tlcolor, tlcolor, blcolor);
                    zoneOutput.AddPhysicsEdge(bg, topIndex, bottomIndex);
                    break;
                case SIDE_RIGHT:
                    zoneOutput.AddQuad(x1y0, xty0_bottom, xty1_top, x1y1, brcolor, brcolor, trcolor, trcolor);
                    zoneOutput.AddPhysicsEdge(bg, bottomIndex, topIndex);
                    break;
                case SIDE_TOP:
                    zoneOutput.AddQuad(x0y1, x1y1, x1yt_right, x0yt_left, tlcolor, trcolor, trcolor, tlcolor);
                    zoneOutput.AddPhysicsEdge(bg, rightIndex, leftIndex);
                    break;
                // single corner false cases 
                case CHIPPED_TOP_RIGHT:
                    zoneOutput.AddFive(x0y0, x0y1, xty1_top, x1yt_right, x1y0, blcolor, tlcolor, tlcolor, brcolor, brcolor);
                    zoneOutput.AddPhysicsEdge(bg, topIndex, rightIndex);
                    break;
                case CHIPPED_BOTTOM_RIGHT:
                    zoneOutput.AddFive(x0y0, x0y1, x1y1, x1yt_right, xty0_bottom, blcolor, tlcolor, trcolor, trcolor, blcolor);
                    zoneOutput.AddPhysicsEdge(bg, rightIndex, bottomIndex);
                    break;
                case CHIPPED_TOP_LEFT:
                    zoneOutput.AddFive(x0y0, x0yt_left, xty1_top, x1y1, x1y0, blcolor, blcolor, trcolor, trcolor, brcolor);
                    zoneOutput.AddPhysicsEdge(bg, leftIndex, topIndex);
                    break;
                case CHIPPED_BOTTOM_LEFT:
                    zoneOutput.AddFive(x0yt_left, x0y1, x1y1, x1y0, xty0_bottom, tlcolor, tlcolor, trcolor, brcolor, brcolor);
                    zoneOutput.AddPhysicsEdge(bg, bottomIndex, leftIndex);
                    break;

                // diagonal cases (2 tris)
                case CORNERS_BOTTOM_LEFT_AND_TOP_RIGHT:
                    zoneOutput.AddTriangle(x0y0, x0yt_left, xty0_bottom, blcolor, blcolor, blcolor);
                    zoneOutput.AddPhysicsEdge(bg, leftIndex, bottomIndex);
                    zoneOutput.AddTriangle(x1y1, x1yt_right, xty1_top, trcolor, trcolor, trcolor);
                    zoneOutput.AddPhysicsEdge(bg, rightIndex, topIndex);
                    break;
                case CORNERS_BOTTOM_RIGHT_AND_TOP_LEFT:
                    zoneOutput.AddTriangle(x1y0, xty0_bottom, x1yt_right, brcolor, brcolor, brcolor);
                    zoneOutput.AddPhysicsEdge(bg, bottomIndex, rightIndex);
                    zoneOutput.AddTriangle(x0y1, xty1_top, x0yt_left, tlcolor, tlcolor, tlcolor);
                    zoneOutput.AddPhysicsEdge(bg, topIndex, leftIndex);
                    break;
            }

            return setMask;
        }

        /*
        void UpdateLightingBlock(int blockx, int blocky)
        {
            if (blockx < 0 || blocky < 0) return;

            int dimy = m_Dimension.Y;
            int dimx = m_Dimension.X;

            int yblocks = (dimy + ZONES_PER_BLOCK_DIM - 1) / ZONES_PER_BLOCK_DIM;
            int xblocks = (dimx + ZONES_PER_BLOCK_DIM - 1) / ZONES_PER_BLOCK_DIM;

            if (blockx >= xblocks || blocky >= yblocks) return;

            int blockid = blockx + blocky * xblocks;

            CreateGeoStart(-blockid);

            int starty = blocky * ZONES_PER_BLOCK_DIM;
            int endy = Math.Min(blocky * ZONES_PER_BLOCK_DIM + ZONES_PER_BLOCK_DIM, dimy);
            int startx = blockx * ZONES_PER_BLOCK_DIM;
            int endx = Math.Min(blockx * ZONES_PER_BLOCK_DIM + ZONES_PER_BLOCK_DIM, dimx);

            float w = m_ZoneWidth;
            float h = m_ZoneHeight;

            for (int y = starty; y < endy; y++)
            {
                for (int x = startx; x < endx; x++)
                {
                    float xw = x * m_ZoneWidth;
                    float yh = y * m_ZoneHeight;

                    Vector3 x0y0 = new Vector2(xw, yh);
                    Vector3 x1y0 = new Vector2(xw + w, yh);
                    Vector3 x0y1 = new Vector2(xw, yh + h);
                    Vector3 x1y1 = new Vector2(xw + w, yh + h);

                    int v00 = 255 - Mathf.FloorToInt( Mathf.Min(1.0f, 2.0f * m_LightingData.GetValue(x, y)) * 255);
                    int v10 = 255 - Mathf.FloorToInt(Mathf.Min(1.0f, 2.0f * m_LightingData.GetValue(x+1, y)) * 255);
                    int v01 = 255 - Mathf.FloorToInt(Mathf.Min(1.0f, 2.0f * m_LightingData.GetValue(x, y+1)) * 255);
                    int v11 = 255 - Mathf.FloorToInt(Mathf.Min(1.0f, 2.0f * m_LightingData.GetValue(x+1, y+1)) * 255);
                    int vmid = (v00 + v10 + v01 + v11) / 4;
                    AddQuad(x0y0, x0y1, x1y1, x1y0,
                        new Color32(255, 0, 0, (byte)v00),
                        new Color32(255, 0, 0, (byte)v01),
                        new Color32(255, 0, 0, (byte)v11),
                        new Color32(255, 0, 0, (byte)v10));
                }
            }
            CreateGeoEnd(-blockid);
        }

        void UpdateLightingBlocks(int gx0, int gy0, int gx1, int gy1)
        {
            // We affect the zone to the left and below of us at least.
            int bx0 = (gx0 - 1) / ZONES_PER_BLOCK_DIM;
            int by0 = (gy0 - 1) / ZONES_PER_BLOCK_DIM;

            // The upper bound will be the zone with coordinates same as grid coords so don't increment.
            int bx1 = gx1 / ZONES_PER_BLOCK_DIM;
            int by1 = gy1 / ZONES_PER_BLOCK_DIM;

            for (int bx = bx0; bx <= bx1; bx++)
            {
                for (int by = by0; by <= by1; by++)
                {
                    UpdateLightingBlock(bx, by);
                }
            }
        }
        */

        protected virtual float SunlightIntensity
        {
            get { return 1.0f; }
        }

        public GameObject m_LightPrefab;
        Dictionary<IntVector2, GameObject> m_CreatedLights = new Dictionary<IntVector2, GameObject>();
        void UpdateLights(Vector3 pos_ws)
        {
            Profiler.Begin();

            var touchedLights = new Dictionary<GameObject, bool>();

            float LIGHT_RANGE = 10.0f;
            float LIGHT_POSITION_Z = -5.0f;
            float LIGHT_INTENSITY_MULTIPLIER = 3.0f;
            int GRID_SIZE = 5;

            var pos_gs = WorldSpaceToGrid(pos_ws);
            var iv = new IntVector2();
            int igsx = Mathf.RoundToInt(pos_gs.x);
            int igsy = Mathf.RoundToInt(pos_gs.y);

            //@TODO get from camera
            int cameraX = 12;
            int cameraY = 12;
            var camMain = Camera.main;
            if(camMain !=null && camMain.orthographic)
            {
                cameraX = Mathf.RoundToInt(camMain.orthographicSize * camMain.aspect);
                cameraY = Mathf.RoundToInt(camMain.orthographicSize * 1.0f);
            }

            // @TODO something wrong with this math, why does it need the extra grid size expansion
            int igsx0 = (igsx - (3 * GRID_SIZE + cameraX / 2)).FloorToMultiple(GRID_SIZE);
            int igsy0 = (igsy - (2 * GRID_SIZE + cameraY / 2)).FloorToMultiple(GRID_SIZE);
            int igsx1 = (igsx + (3 * GRID_SIZE + cameraX / 2)).CeilToMultiple(GRID_SIZE);
            int igsy1 = (igsy + (2 * GRID_SIZE + cameraX / 2)).CeilToMultiple(GRID_SIZE);

            for (int x= igsx0; x<= igsx1; x+= GRID_SIZE)
            {
                for (int y = igsy0; y <= igsy1; y+= GRID_SIZE)
                {
                    iv.Reinit(x, y);
                    Vector3 cur_ws = GridSpaceToWorld(new Vector3(iv.X + GRID_SIZE*0.5f, iv.Y + GRID_SIZE * 0.5f, 0.0f));
                    cur_ws.z = LIGHT_POSITION_Z;
                    GameObject go;
                    if (!m_CreatedLights.TryGetValue(iv, out go))
                    {
                        go = GameObject.Instantiate<GameObject>(m_LightPrefab, cur_ws, Quaternion.identity);
                        m_CreatedLights[iv] = go;
                    }

                    // Setup light
                    touchedLights[go] = true;
                    var light = go.GetComponent<Light>();

                    float finalIntensity = 0.0f;
                    float sunIntensity = SunlightIntensity;
                    for (int xx=iv.X; xx < iv.X+GRID_SIZE; xx++)
                    {
                        for(int yy= iv.Y; yy < iv.Y+GRID_SIZE; yy++)
                        {
                            var pointIntensity = m_LightingData.GetValue(xx,yy);
                            {
                                //foreach (var vlight in ComponentList.GetCopiedListOfType<VoxelLight>())
                                {
                                 //   var individualContrib = m_LightingData.GetValueFromDict(xx,yy, vlight.Contribution);
                                 //   pointIntensity += individualContrib;
                                }
                            }
                            pointIntensity = Mathf.Min(1.0f, pointIntensity);

                            finalIntensity += pointIntensity;
                        }
                    }

                    light.intensity = LIGHT_INTENSITY_MULTIPLIER * (finalIntensity / (GRID_SIZE * GRID_SIZE));
                    light.range = LIGHT_RANGE;
                }
            }

            // Destroy untouched lights
            var toDestroy = new List<IntVector2>();
            foreach(var pair in m_CreatedLights)
            {
                bool outVal;
                if(!touchedLights.TryGetValue(pair.Value, out outVal))
                {
                    toDestroy.Add(pair.Key);
                }
            }
            foreach(var gob in toDestroy)
            {
                Destroy(m_CreatedLights[gob]);
                m_CreatedLights.Remove(gob);
            }
            Profiler.End();
        }

        void DirtyOrUpdateBlocksOverlappingGridPointsInclusive(bool dirty, int gx0, int gy0, int gx1, int gy1)
        {
            // We affect the zone to the left and below of us at least.
            int bx0 = (gx0 - 1) / ZONES_PER_BLOCK_DIM;
            int by0 = (gy0 - 1) / ZONES_PER_BLOCK_DIM;

            // The upper bound will be the zone with coordinates same as grid coords so don't increment.
            int bx1 = gx1 / ZONES_PER_BLOCK_DIM;
            int by1 = gy1 / ZONES_PER_BLOCK_DIM;

            for (int bx = bx0; bx <= bx1; bx++)
            {
                for (int by = by0; by <= by1; by++)
                {
                    if (dirty)
                    {
                        m_BlocksClean[bx, by] = false;
                    }
                    else
                    {
                        UpdateBlock(bx, by);
                    }
                }
            }

            if (dirty)
            {
                // Notify listeners
                foreach (var gp in ComponentList.GetCopiedListOfType<IGridPointsDirty>())
                {
                    gp.GridPointsDirty(new GridPointsDirtyArgs(this, GridSpaceToWorld(new Vector3(gx0, gy0)), GridSpaceToWorld(new Vector3(gx1, gy1))));
                }
            }
        }

        bool IsBlockClear(int x, int y)
        {
            IntVector2 zone = new IntVector2(x, y);
            for (int layerIndex = 0; layerIndex < m_Layers.Count; layerIndex++)
            {
                var layer = m_Layers[layerIndex];
                float bl = layer.GetValue(zone);
                float br = layer.GetValue(new IntVector2(zone.X + 1, zone.Y));
                float ul = layer.GetValue(new IntVector2(zone.X, zone.Y + 1));
                float ur = layer.GetValue(new IntVector2(zone.X + 1, zone.Y + 1));

                if (bl > ZERO_CROSSING_VALUE || br > ZERO_CROSSING_VALUE || ul > ZERO_CROSSING_VALUE || ur > ZERO_CROSSING_VALUE) return false;
            }

            return true;
        }

        void UpdateBlock(int blockx, int blocky)
        {
            if (blockx < 0 || blocky < 0) return;

            int dimy = m_Dimension.Y;
            int dimx = m_Dimension.X;

            int yblocks = (dimy + ZONES_PER_BLOCK_DIM - 1) / ZONES_PER_BLOCK_DIM;
            int xblocks = (dimx + ZONES_PER_BLOCK_DIM - 1) / ZONES_PER_BLOCK_DIM;

            if (blockx >= xblocks || blocky >= yblocks) return;

            if (m_BlocksClean[blockx, blocky]) return;
            m_BlocksClean[blockx, blocky] = true;

            int blockid = blockx + blocky * xblocks;


            float w = m_ZoneWidth;
            float h = m_ZoneHeight;

            int starty = blocky * ZONES_PER_BLOCK_DIM;
            int endy = Math.Min(blocky * ZONES_PER_BLOCK_DIM + ZONES_PER_BLOCK_DIM, dimy);
            int startx = blockx * ZONES_PER_BLOCK_DIM;
            int endx = Math.Min(blockx * ZONES_PER_BLOCK_DIM + ZONES_PER_BLOCK_DIM, dimx);

            // Clear physics
            m_PhysicsEdges.Clear();
            m_VertList.Clear();

            // Check if we have nothing in there- if so, don't bother making this object.
            bool blockClear = true;
            for (int y = starty; y < endy; y++)
            {
                if (!blockClear) break;

                for (int x = startx; x < endx; x++)
                {
                    if (!IsBlockClear(x, y))
                    {
                        blockClear = false;
                        break;
                    }
                }
            }

            if (blockClear)
            {
                DeleteGeoBlock(blockid);
                return;
            }

            CreateGeoStart(blockid);
            for (int layerIndex = 0; layerIndex < m_Layers.Count; layerIndex++)
            {
                var layer = m_Layers[layerIndex];
                // Clear physics vert cache since it's different per each layer (but we still want to add to the edges and vert lists.
                for (int y = starty; y < endy + 1; y++)
                {
                    for (int x = startx; x < endx + 1; x++)
                    {
                        for (int k = 0; k < m_GridIndices.GetLength(2); k++)
                            m_GridIndices[x, y, k] = -1;
                    }
                }

                for (int y = starty; y < endy; y++)
                {
                    for (int x = startx; x < endx; x++)
                    {
                        bool black = false; // @TEST (blockx + blocky) % 2 == 0;
                        if (layerIndex == 1) black = true;

                        GenerateQuadForZone(layer, new IntVector2(x, y), black, m_ZoneOutput);
                    }
                }
            }
            CreateGeoEnd(blockid);
            AddPhysics(blockid);
        }

        void UpdateAllBlocks()
        {
            float w = m_ZoneWidth;
            float h = m_ZoneHeight;

            int dimy = m_Dimension.Y;
            int dimx = m_Dimension.X;
            int yblocks = (dimy + ZONES_PER_BLOCK_DIM - 1) / ZONES_PER_BLOCK_DIM;
            int xblocks = (dimx + ZONES_PER_BLOCK_DIM - 1) / ZONES_PER_BLOCK_DIM;
            //@TEST only doing some of the blocks to run faster
            int starty = 0;
            for (int blocky = starty; blocky < yblocks; blocky++)
            {
                for (int blockx = 0; blockx < xblocks; blockx++)
                {
                    // if ((blockid) % 2 == 0) continue; // @TEST Checkerboard test

                    UpdateBlock(blockx, blocky);
                }
            }
        }

        public void DebugToolClick(int mouseButtonPressed, Vector3 shootingTarget_ws)
        {
            Vector3 v_gs = WorldSpaceToGrid(shootingTarget_ws);

            int gx = Mathf.RoundToInt(v_gs.x);
            int gy = Mathf.RoundToInt(v_gs.y);
            foreach (var layer in m_Layers)
            {
                layer.SetValue(gx, gy, mouseButtonPressed == 0 ? 1.0f : -1.0f);
            }

            DirtyOrUpdateBlocksOverlappingGridPointsInclusive(true, gx, gy, gx, gy);
        }

        public bool IsBoxClearAt(Vector3 center_ws, Vector3 up_ws, float halfWidth, float halfHeight)
        {
            Vector3 wh_gs = WorldSpaceVectorToGrid(new Vector3(halfWidth, halfHeight, 0.0f));
            Vector3 center_gs = WorldSpaceToGrid(center_ws);
            foreach(var layer in m_Layers)
            {
                if (layer.Background) continue;
                if (!layer.IsBoundingBoxClear(center_gs, up_ws, wh_gs.x, wh_gs.y)) return false;
            }

            return true;
        }

        public bool IsPointClearAt_gs(Vector3 point_gs, bool allLayers=true)
        {
            for (int i=0; i<(allLayers ? m_Layers.Count : 1);i++)
            {
                var layer = m_Layers[i];
                if (layer.Background) continue;

                var value = layer.GetValueAtZone(new IntVector2(Mathf.FloorToInt(point_gs.x), Mathf.FloorToInt(point_gs.y)), new Vector2(point_gs.x % 1.0f, point_gs.y % 1.0f));
                if (value > ZERO_CROSSING_VALUE) return false;
            }

            return true;
        }

        public bool IsPointClearAt_ws(Vector3 point_ws, bool allLayers = true)
        {
            Vector3 point_gs = WorldSpaceToGrid(point_ws);
            return IsPointClearAt_gs(point_gs, allLayers);
        }

        /*
        public Dictionary<IntVector2, float> ComputeLightContribution( Vector3 pos_ws, float light, float falloff)
        {
            return m_LightingData.ComputeLightContribution(this, pos_ws, light, falloff);
        }
        */

        public void AddSunLightPoint(Vector3 shootingTarget_ws)
        {
            Vector3 center_gs = WorldSpaceToGrid(shootingTarget_ws);
            m_LightingData.SetValue(Mathf.FloorToInt(center_gs.x), Mathf.FloorToInt(center_gs.y), 1.0f);
        }

        public void ModifyCircle(float digPower, bool additive, Vector3 shootingTarget_ws, float radius, bool exactPower=false)
        {
            Vector3 radius_gs = WorldSpaceVectorToGrid(new Vector3(radius, radius));
            Vector3 center_gs = WorldSpaceToGrid(shootingTarget_ws);
            Vector3 ll_gs = WorldSpaceToGrid(shootingTarget_ws + radius * new Vector3(-1.0f, -1.0f)) + new Vector3(-2.0f, -2.0f);
            Vector3 ur_gs = WorldSpaceToGrid(shootingTarget_ws + radius * new Vector3(1.0f, 1.0f)) + new Vector3(2.0f, 2.0f);

            IntVector2 vi_ll_gs = new IntVector2(ll_gs);
            IntVector2 vi_ur_gs = new IntVector2(ur_gs);

            foreach (var layer in m_Layers)
            {
                if (!exactPower)
                {
                    if (additive && layer.DigPower != digPower) continue;
                    if (!additive && layer.DigPower > digPower) continue;
                }
                else
                {
                    if (layer.DigPower != digPower) continue;
                }

                for (int x = vi_ll_gs.X; x <= vi_ur_gs.X; x++)
                {
                    for (int y = vi_ll_gs.Y; y <= vi_ur_gs.Y; y++)
                    {
                        var oldvalue = layer.GetValue(new IntVector2(x, y));
                        float distanceToCircleEdge_gs = ((new Vector3(x, y) - center_gs).magnitude - radius_gs.x);
                        float newValue;

                        newValue = (additive ? -1.0f : 1.0f) * distanceToCircleEdge_gs;

                        // @TEMP To get closer to a circle shape if we weren't going to change the number still allow going a little closer to it.
                        //if (Mathf.Sign(newValue) == Mathf.Sign(oldvalue) && Mathf.Abs(newValue-oldvalue)<0.1f)
                        {
                            //newValue = oldvalue;
                        }

                        if (additive)
                        {
                            if (newValue < oldvalue)
                            {
                                newValue = oldvalue;
                            }

                        }
                        else
                        {
                            if (newValue > oldvalue)
                            {
                                newValue = oldvalue;
                            }
                        }
                        layer.SetValue(x, y, newValue);// additive ? Mathf.Max(newValue, oldvalue) : Mathf.Min(newValue, oldvalue));
                    }
                }
                layer.CleanScraps(vi_ll_gs, vi_ur_gs);
            }

            DirtyOrUpdateBlocksOverlappingGridPointsInclusive(true, vi_ll_gs.X, vi_ll_gs.Y, vi_ur_gs.X, vi_ur_gs.Y);
        }

        public void OnDestroy()
        {
            ComponentList.OnEnd(this);
        }

        public class LightingData
        {
            IntVector2 m_Dimension;
            float[,] m_Values;
            public byte[,] m_LitValues;

            public void SetValue(int x, int y, float v)
            {
                m_Values[x, y] = v;
            }

            public float GetValue(int x, int y)
            {
                if (x < 0 || y < 0) return 1.0f;
                if (x >= m_Dimension.X || y >= m_Dimension.Y) return 1.0f;
                if (m_Values != null)
                {
                    return m_Values[x, y];
                }
                return 1.0f;
            }

            public void Resize(int ix, int iy)
            {
                m_Dimension = new IntVector2(ix, iy);
                m_Values = new float[ix, iy];
                m_LitValues = new byte[ix, iy];
                {
                    for (int y = 0; y < m_Values.GetLength(1); y++)
                        for (int x = 0; x < m_Values.GetLength(0); x++)
                        {
                            m_Values[x, y] = 0.0f;
                            m_LitValues[x, y] = 0;
                        }
                }

                ComputeInitialLighting();
            }

            public class Entry
            {
                public IntVector2 m_Location;
                public float m_Value;

                public Entry(IntVector2 loc, float value)
                {
                    m_Location = loc;
                    m_Value = value;
                }
            };

            void InsertQueue(List<Entry> queue, float value, IntVector2 pos)
            {
                int i = 0;
                while(i < queue.Count)
                {
                    if (queue[i].m_Value <= value)
                    {
                        queue.Insert(i, new Entry(pos, value));
                        return;
                    }
                    i++;
                }

                // Needs to go at the end.
                queue.Add(new Entry(pos, value));
            }

            bool IsExploredOrGround(bool[,] explored, int x, int y)
            {
                if (m_Gen.m_Layers[0].GetValue(x, y) > 0.0f) return true;
                if (x < 0 || y < 0) return true;
                if (x >= m_Dimension.X || y >= m_Dimension.Y) return true;
                if (explored[x, y]) return true;

                return false;
            }

            VoxelGenerator m_Gen;
            public bool ComputeLightingStep(int xm, int ym, int xr, int yr, VoxelGenerator gen)
            {
                Profiler.Begin();
                float sunIntensity = gen.SunlightIntensity;
                float SUN_LIGHT_FALLOFF = 0.04f;

                int dimx = gen.m_DimensionX;
                int dimy = gen.m_DimensionY;
                int x0 = xm - xr;
                int x1 = xm + xr;
                int y0 = ym - yr;
                int y1 = ym + yr;
                if (x0 < gen.m_ClipX0) x0 = gen.m_ClipX0;
                if (x1 > gen.m_ClipX1) x1 = gen.m_ClipX1;
                if (y0 < gen.m_ClipY0) y0 = gen.m_ClipY0;
                if (y1 > gen.m_ClipY1) y1 = gen.m_ClipY1;

                bool changed = false;
                var layer = gen.m_Layers[0];
                for (int x=x0;x<x1;x++)
                {
                    for(int y=y0;y<y1;y++)
                    {
                        // dirt there
                        if (gen.m_Layers[0].GetValue(x, y) > ZERO_CROSSING_VALUE)
                        {
                            m_Values[x, y] = 0.0f;
                            continue;
                        }
                        float finalValue = 0.0f;
                        var litValue = m_LitValues[x, y];

                        if (0 != (litValue & 1))
                        {
                            finalValue = sunIntensity;
                        }

                        if(0 != (litValue & 2))
                        {
                            finalValue = Mathf.Max(finalValue, 1.0f);
                        }

                        float a = (m_Values[x + 1, y] - SUN_LIGHT_FALLOFF);
                        finalValue = Math.Max(a, finalValue);
                        float b = (m_Values[x - 1, y] - SUN_LIGHT_FALLOFF);
                        finalValue = Math.Max(b, finalValue);
                        float c = (m_Values[x, y + 1] - SUN_LIGHT_FALLOFF);
                        finalValue = Math.Max(c, finalValue);
                        float d = (m_Values[x, y - 1] - SUN_LIGHT_FALLOFF);
                        finalValue = Math.Max(d, finalValue);
                        if (m_Values[x, y] != finalValue)
                        {
                            // changed
                            m_Values[x, y] = finalValue;
                        }                        
                    }
                }

                Profiler.End();
                return changed;
            }

            public bool SetLight(Vector3 light_gs, bool set)
            {
                var lighti_gs = new IntVector2(light_gs);
                if (set)
                {
                    byte oldValue = m_LitValues[lighti_gs.X, lighti_gs.Y];
                    if (0 != (oldValue & 2)) return false;
                    m_LitValues[lighti_gs.X, lighti_gs.Y] ^= 2;
                }
                else
                {
                    m_LitValues[lighti_gs.X, lighti_gs.Y] &= 0xfd;
                }

                return true;
            }

            public void AddSunSeedPoint(int x, int y)
            {
                m_LitValues[x, y] ^= 1;
            }

            public void ComputeInitialLighting()
            {
            }
        }

        LightingData m_LightingData = new LightingData();
        public LightingData LightingDataInstance
        {
            get { return m_LightingData; }
        }


        protected virtual void VirtualAwake()
        {
        }

        public void Awake()
        {
            ComponentList.OnStart(this);

            m_Dimension = new IntVector2(m_DimensionX, m_DimensionY);
            // Place earth to be centered around 0,0
            m_ClipX0 += m_DimensionX / 2; m_ClipX0 = m_ClipX0.Clamp(0, m_DimensionX - 1);
            m_ClipX1 += m_DimensionX / 2; m_ClipX1 = m_ClipX1.Clamp(0, m_DimensionX - 1);
            m_ClipY0 += m_DimensionY / 2; m_ClipY0 = m_ClipY0.Clamp(0, m_DimensionY - 1);
            m_ClipY1 += m_DimensionY / 2; m_ClipY1 = m_ClipY1.Clamp(0, m_DimensionY - 1);

            Resize(m_Dimension.X, m_Dimension.Y);

            VirtualAwake();
        }

        int index = 0;
        public virtual void VirtualUpdate()
        {
        }

        void RunNUpdates(int updatesAllowed)
        {
            for (int x = 0; x < m_BlocksX; x++)
                for (int y = 0; y < m_BlocksY; y++)
                {
                    if (!m_BlocksClean[x, y])
                    {
                        UpdateBlock(x, y);

                        updatesAllowed--;
                        if (updatesAllowed <= 0)
                            return;
                    }
                }
        }

        public void Update()
        {
            VirtualUpdate();

            var pp = new Vector3();
            foreach (var player in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.ThirdPersonUserControl>())
            {
                pp = player.transform.position;
            }

            Vector3 playerPos_gs = WorldSpaceToGrid(pp);

            // Update blocks near player
            {
                int gx = Mathf.RoundToInt(playerPos_gs.x);
                int gy = Mathf.RoundToInt(playerPos_gs.y);

                DirtyOrUpdateBlocksOverlappingGridPointsInclusive(false, gx-9, gy-9, gx+9, gy+9);
                //UpdateLightingBlocks(gx - 9, gy - 9, gx + 9, gy + 9);
                UpdateLights(pp);
            }

            int updatesAllowed = 33;
            RunNUpdates(updatesAllowed);

            //if (!m_LightingStabilized)
            //    m_LightingStabilized = !m_LightingData.ComputeLightingStep(this);
            var camMain = Camera.main;
            int cameraX = 25;
            int cameraY = 20;
            if (camMain != null && camMain.orthographic)
            {
                cameraX = 2 * Mathf.RoundToInt(camMain.orthographicSize * camMain.aspect);
                cameraY = 2 * Mathf.RoundToInt(camMain.orthographicSize * 1.0f);
            }
            m_LightingData.ComputeLightingStep((int)playerPos_gs.x, (int)playerPos_gs.y, cameraX, cameraY, this);

            // @TEST BELOW: individual block update for test
            return;

            int dimy = m_Dimension.Y;
            int dimx = m_Dimension.X;
            int yblocks = (dimy + ZONES_PER_BLOCK_DIM - 1) / ZONES_PER_BLOCK_DIM;
            int xblocks = (dimx + ZONES_PER_BLOCK_DIM - 1) / ZONES_PER_BLOCK_DIM;
            UpdateBlock(index % xblocks, index / xblocks);

            index++;
            if (index > xblocks * yblocks) index = 0;
        }
        bool m_LightingStabilized = false;

        



        class EarthGenerateFlat
        {
            public EarthGenerateFlat(VoxelGenerator gen)
            {
                m_Gen = gen;
            }

            void GenerateHeightsFlat()
            {
                m_Heights = new float[m_Gen.m_DimensionX];
                var perlinFactor = 4.1f / m_Gen.m_DimensionX;

                for (int i = 0; i < m_Heights.Length; i++)
                {
                    m_Heights[i] = Mathf.Lerp(0.5f, 1.0f, Mathf.PerlinNoise(perlinFactor * i, 1.0f));
                }

                // Flatten out the starting area
                int center = m_Heights.Length / 2;
                int flattenSize = m_Heights.Length / 10;
                for (int i = 0; i < flattenSize; i++)
                {
                    var t = (i * 1.0f) / flattenSize;
                    m_Heights[center + i] = Mathf.Lerp(0.5f, m_Heights[center + i], t);
                    m_Heights[center - i] = Mathf.Lerp(0.5f, m_Heights[center - i], t);
                }
            }

            TwoDee.RandomGeneratorUnity m_Rng;
            void GenerateFlat(Layer layer)
            {
                float perlinFactor = 8;// 1600.0f * (1.0f / m_Gen.m_Dimension.X);
                float xOrg = m_Rng.Range(1.0f, 10.0f) * 10.0f;
                float yOrg = m_Rng.Range(1.0f, 10.0f) * 10.0f;
                float noiseScale = 0.1f;
                // Can't put values on the very edges because they will have incomplete physics- the leftmost zone for example would be interior if all its grid points were set.
                float halfDimX = m_Gen.m_Dimension.X / 2;
                float halfDimY = m_Gen.m_Dimension.Y / 2;

                var values = new float[m_Gen.m_Dimension.X, m_Gen.m_Dimension.Y];

                for (int y = 1; y < values.GetLength(1) - 1; y++)
                {
                    float ynorm = (y * 1.0f) / values.GetLength(1);
                    for (int x = 1; x < values.GetLength(0) - 1; x++)
                    {
                        float xCoord = xOrg + x / (values.GetLength(0) * noiseScale);
                        float yCoord = yOrg + y / (values.GetLength(1) * noiseScale);

                        values[x, y] = 0.07f + (ZERO_CROSSING_VALUE - 0.5f) + Mathf.PerlinNoise(perlinFactor * xCoord, perlinFactor * yCoord);

                        // hardrock
                        if (layer.DigPower > 1.0f)
                        {
                            values[x, y] -= ynorm * 0.3f;// 0.1f;
                        }

                        float allowedHeight = m_Gen.m_Dimension.Y * m_Heights[x];
                        if (y > allowedHeight)
                        {
                            values[x, y] = 0.0f;
                        }
                    }
                }

                layer.m_Values = values;
            }


            float[] m_Heights;

            public void Generate()
            {
                GenerateHeightsFlat();

                foreach (var layer in m_Gen.m_Layers)
                {
                    RadialGravity.m_RadialEnabled = true;
                    GenerateFlat(layer);

                    var filter = new Gaussian5x5BlurFilter();
                    var values2 = layer.m_Values.ConvolutionFilter<float, Gaussian5x5BlurFilter>(filter);
                    layer.m_Values = values2;
                }
            }

            VoxelGenerator m_Gen;
        }
    }
}