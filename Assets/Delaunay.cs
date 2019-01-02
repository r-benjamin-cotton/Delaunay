// http://cottonia-cotton.cocolog-nifty.com/
// Copyright(c) 2019 benjamin.
using System;
using System.Collections.Generic;

namespace MySpace
{
    /// <summary>
    /// ドローネ三角形分割
    /// </summary>
    public class Delaunay
    {
        /// <summary>
        /// 頂点
        /// </summary>
        public struct Point
        {
            /// <summary>
            /// ｘ座標
            /// </summary>
            public float x;
            /// <summary>
            /// y座標
            /// </summary>
            public float y;
        }

        /// <summary>
        /// 辺
        /// </summary>
        public struct Edge
        {
            /// <summary>
            /// 頂点0のインデックス
            /// </summary>
            public int p0;
            /// <summary>
            /// 頂点1のインデックス
            /// </summary>
            public int p1;
            /// <summary>
            /// 辺を共有する三角形0
            /// p0->p1->t0 cw
            /// </summary>
            public int t0;
            /// <summary>
            /// 辺を共有する三角形1
            /// p0->p1->t1 ccw
            /// </summary>
            public int t1;
        }
        /// <summary>
        /// 三角形
        /// </summary>
        public struct Triangle
        {
            /// <summary>
            /// 頂点0のインデックス
            /// p0->p1->p2 ccw
            /// </summary>
            public int p0;
            /// <summary>
            /// 頂点1のインデックス
            /// p0->p1->p2 ccw
            /// </summary>
            public int p1;
            /// <summary>
            /// 頂点2のインデックス
            /// p0->p1->p2 ccw
            /// </summary>
            public int p2;
            /// <summary>
            /// 辺0(p0-p1)のインデックス
            /// </summary>
            public int e0;      // p0-p1
            /// <summary>
            /// 辺1(p1-p2)のインデックス
            /// </summary>
            public int e1;      // p1-p2
            /// <summary>
            /// 辺2(p2-p0)のインデックス
            /// </summary>
            public int e2;      // p2-p0
        }

        /// <summary>
        /// 計算機イプシロン
        /// </summary>
        private const float FltEpsilon = 1.192092896e-07f;

        private int numPoints;
        private int numEdges;
        private int numTriangles;
        private readonly Point[] points;          // n
        private readonly Edge[] edges;            // n * 3 １頂点追加で３っつ増加
        private readonly Triangle[] triangles;    // n * 2 １頂点追加で３っつ増加１つ削除
        private readonly Stack<int> edgeStack = new Stack<int>();

        /// <summary>
        /// 頂点リスト、Lengthは構築時のmax
        /// 割り当て数はNumPoints。
        /// </summary>
        public Point[] Points
        {
            get
            {
                return points;
            }
        }
        /// <summary>
        /// 頂点リストの割り当て数
        /// </summary>
        public int NumPoints
        {
            get
            {
                return numPoints;
            }
        }
        /// <summary>
        /// 辺リスト、Lengthは構築時のmax*3
        /// 割り当て数はNumEdges。
        /// </summary>
        public Edge[] Edges
        {
            get
            {
                return edges;
            }
        }
        /// <summary>
        /// 辺リストの割り当て数
        /// </summary>
        public int NumEdges
        {
            get
            {
                return numEdges;
            }
        }
        /// <summary>
        /// 三角形リスト、Lengthは構築時のmax*2
        /// 割り当て数はNumTriangles。
        /// </summary>
        public Triangle[] Triangles
        {
            get
            {
                return triangles;
            }
        }
        /// <summary>
        /// 三角形リストの割り当て数
        /// </summary>
        public int NumTriangles
        {
            get
            {
                return numTriangles;
            }
        }
        /// <summary>
        /// ドローネクラスコンストラクタ
        /// maxは最大長点数
        /// </summary>
        /// <param name="max"></param>
        public Delaunay(int max)
        {
            if (max < 4)
            {
                throw new ArgumentOutOfRangeException();
            }
            points = new Point[max];
            edges = new Edge[max * 3];
            triangles = new Triangle[max * 2];
        }
        /// <summary>
        /// 内部情報クリア
        /// </summary>
        public void Clear()
        {
            numPoints = 0;
            numEdges = 0;
            numTriangles = 0;
            edgeStack.Clear();
        }

        // n0->n1->n2がCCWになっていれば1、線上なら0、CWなら-1
        private int CheckCCW(int p0, int p1, int p2)
        {
            var n0x = points[p0].x;
            var n0y = points[p0].y;
            var n1x = points[p1].x;
            var n1y = points[p1].y;
            var n2x = points[p2].x;
            var n2y = points[p2].y;
            var x1 = n1x - n0x;
            var y1 = n1y - n0y;
            var x2 = n2x - n0x;
            var y2 = n2y - n0y;
            var z = x1 * y2 - x2 * y1;
            return (z > +FltEpsilon) ? +1 : ((z < -FltEpsilon) ? -1 : 0);
        }
        // フリップ可能な四角形(n0-n2は対角)ならtrue,それ以外はfalse
        private bool Flippable(int p0, int p1, int p2, int p3)
        {
            var ccw = CheckCCW(p1, p2, p3);
            if (ccw != CheckCCW(p3, p0, p1))
            {
                return false;
            }
#if false // n0-n2が辺を成していることを前提とし、計算を削減
            if (ccw != CheckCCW(p2, p3, p0))
            {
                return false;
            }
            if (ccw != CheckCCW(p0, p1, p2))
            {
                return false;
            }
#endif
            return true;
        }
        // n0,n1,n2の外接三角形内にn3が含まれていないとき＝ドローネ条件を満たすときtrue,それ以外はfalse
        private bool CheckDelaunay(int p0, int p1, int p2, int p3)
        {
            var n0x = points[p0].x;
            var n0y = points[p0].y;
            var n1x = points[p1].x;
            var n1y = points[p1].y;
            var n2x = points[p2].x;
            var n2y = points[p2].y;
            var n3x = points[p3].x;
            var n3y = points[p3].y;
            var x0m = n0x - n3x;
            var x1m = n1x - n3x;
            var x2m = n2x - n3x;
            var y0m = n0y - n3y;
            var y1m = n1y - n3y;
            var y2m = n2y - n3y;
            var x0p = n0x + n3x;
            var x1p = n1x + n3x;
            var x2p = n2x + n3x;
            var y0p = n0y + n3y;
            var y1p = n1y + n3y;
            var y2p = n2y + n3y;
            var v =
                +(x0m * x0p + y0m * y0p) * (x1m * y2m - x2m * y1m)
                + (x1m * x1p + y1m * y1p) * (x2m * y0m - x0m * y2m)
                + (x2m * x2p + y2m * y2p) * (x0m * y1m - x1m * y0m);
            return v < +FltEpsilon;
        }

        private void SetTriangle(int te, int p0, int p1, int p2, int e0, int e1, int e2)
        {
            Triangle t;
            t.p0 = p0;
            t.p1 = p1;
            t.p2 = p2;
            t.e0 = e0;
            t.e1 = e1;
            t.e2 = e2;
            triangles[te] = t;
        }
        private int AddEdge(int p0, int p1, int t0, int t1)
        {
            var ee = numEdges++;
            Edge e;
            e.p0 = p0;
            e.p1 = p1;
            e.t0 = t0;
            e.t1 = t1;
            edges[ee] = e;
            return ee;
        }
        private int AddPoint(float x, float y)
        {
            var pp = numPoints++;
            Point p;
            p.x = x;
            p.y = y;
            points[pp] = p;
            return pp;
        }
        // nnが含まれる三角形のインデックスを返す、見つからない場合は-1
        private int FindTriangle(int px)
        {
            var te = numTriangles - 1;
            var ep = numEdges;
            for (; ; )
            {
                var p0 = triangles[te].p0;
                var p1 = triangles[te].p1;
                var p2 = triangles[te].p2;
                var e0 = triangles[te].e0;
                var e1 = triangles[te].e1;
                var e2 = triangles[te].e2;
                if ((ep != e0) && (0 > CheckCCW(p0, p1, px)))
                {
                    var t0 = edges[e0].t0;
                    var t1 = edges[e0].t1;
                    ep = e0;
                    te = (te != t0) ? t0 : t1;
                    if (te < 0)
                    {
                        break;
                    }
                    continue;
                }
                if ((ep != e1) && (0 > CheckCCW(p1, p2, px)))
                {
                    var t0 = edges[e1].t0;
                    var t1 = edges[e1].t1;
                    ep = e1;
                    te = (te != t0) ? t0 : t1;
                    if (te < 0)
                    {
                        break;
                    }
                    continue;
                }
                if ((ep != e2) && (0 > CheckCCW(p2, p0, px)))
                {
                    var t0 = edges[e2].t0;
                    var t1 = edges[e2].t1;
                    ep = e2;
                    te = (te != t0) ? t0 : t1;
                    if (te < 0)
                    {
                        break;
                    }
                    continue;
                }
                return te;
            }
            return -1;
        }

        private bool Setup()
        {
            var p0 = 0;
            var p1 = 1;
            var p2 = 2;
            var p3 = 3;
            var inside = false;
            if (0 > CheckCCW(p0, p1, p2))
            {
                var pt = p1;
                p1 = p2;
                p2 = pt;
            }
            if (0 > CheckCCW(p0, p1, p3))
            {
                var pt = p2;
                p2 = p0;
                p0 = p1;
                p1 = pt;
            }
            else if (0 > CheckCCW(p1, p2, p3))
            {
                var pt = p0;
                p0 = p2;
                p2 = p1;
                p1 = pt;
            }
            else if (0 > CheckCCW(p2, p0, p3))
            {
                // nop
            }
            else
            {
                inside = true;
            }

            if (inside)
            {
                AddEdge(p0, p1, -1, 0);
                AddEdge(p1, p2, -1, 0);
                AddEdge(p2, p0, -1, 0);
                SetTriangle(0, p0, p1, p2, 0, 1, 2);
                numTriangles = 1;
            }
            else
            {
                AddEdge(p0, p1, -1, 0);
                AddEdge(p1, p2, -1, 0);
                AddEdge(p0, p2, 0, 1);
                AddEdge(p2, p3, -1, 1);
                AddEdge(p3, p0, -1, 1);
                SetTriangle(0, p0, p1, p2, 0, 1, 2);
                SetTriangle(1, p0, p2, p3, 2, 3, 4);
                numTriangles = 2;
            }
            return inside;
        }
        // 新しい頂点により、三角形を分割
        private void DivideTrinangle(int px, int tt)
        {
            var p0 = triangles[tt].p0;
            var p1 = triangles[tt].p1;
            var p2 = triangles[tt].p2;
            var e0 = triangles[tt].e0;   // p0-p1
            var e1 = triangles[tt].e1;   // p1-p2
            var e2 = triangles[tt].e2;   // p2-p0
            var t0 = tt;
            var t1 = numTriangles++;
            var t2 = numTriangles++;
            // エッジの対応する三角形を張替え
            if (edges[e0].t0 == tt)
            {
                edges[e0].t0 = t0;
            }
            if (edges[e0].t1 == tt)
            {
                edges[e0].t1 = t0;
            }
            if (edges[e1].t0 == tt)
            {
                edges[e1].t0 = t1;
            }
            if (edges[e1].t1 == tt)
            {
                edges[e1].t1 = t1;
            }
            if (edges[e2].t0 == tt)
            {
                edges[e2].t0 = t2;
            }
            if (edges[e2].t1 == tt)
            {
                edges[e2].t1 = t2;
            }
            // 辺を追加
            // ※新しく出来た辺はドローネ条件を満たしている。
            var e3 = AddEdge(p0, px, t0, t2);
            var e4 = AddEdge(p1, px, t1, t0);
            var e5 = AddEdge(p2, px, t2, t1);
            // 新しく出来た三角形
            SetTriangle(t0, p0, p1, px, e0, e4, e3);
            SetTriangle(t1, p1, p2, px, e1, e5, e4);
            SetTriangle(t2, p2, p0, px, e2, e3, e5);
            // 元の三角形の辺は条件を満たさなくなっている可能性があるので登録。
            edgeStack.Push(e0);
            edgeStack.Push(e1);
            edgeStack.Push(e2);
        }
        // 検査対象が無くなるまでパタパタ。
        private void FlipFlop(int px)
        {
            do
            {
                var ee = edgeStack.Pop();
                var t0 = edges[ee].t0;
                var t1 = edges[ee].t1;
                var p0 = edges[ee].p0;
                var p1 = 0;
                var p2 = edges[ee].p1;
                var p3 = 0;
                var e0 = 0;
                var e1 = 0;
                var e2 = 0;
                var e3 = 0;
                // 外側の辺はスキップ
                if ((t0 < 0) || (t1 < 0))
                {
                    continue;
                }
                // 辺の両側の頂点をみつける。
                if (triangles[t0].e0 == ee)
                {
                    p1 = triangles[t0].p2;
                    e0 = triangles[t0].e1;
                    e1 = triangles[t0].e2;
                }
                else if (triangles[t0].e1 == ee)
                {
                    p1 = triangles[t0].p0;
                    e0 = triangles[t0].e2;
                    e1 = triangles[t0].e0;
                }
                else // if(triangle[t0].e2 == ee)
                {
                    p1 = triangles[t0].p1;
                    e0 = triangles[t0].e0;
                    e1 = triangles[t0].e1;
                }
                if (triangles[t1].e0 == ee)
                {
                    p3 = triangles[t1].p2;
                    e2 = triangles[t1].e1;
                    e3 = triangles[t1].e2;
                }
                else if (triangles[t1].e1 == ee)
                {
                    p3 = triangles[t1].p0;
                    e2 = triangles[t1].e2;
                    e3 = triangles[t1].e0;
                }
                else// if(triangle[t1].e2 == ee)
                {
                    p3 = triangles[t1].p1;
                    e2 = triangles[t1].e0;
                    e3 = triangles[t1].e1;
                }
                // フリップ不可能であればスキップ。
                if (!Flippable(p0, p1, p2, p3))
                {
                    continue;
                }
                // ドローネ条件を満たしていればスキップ。
                if (p1 == px)
                {
                    if (CheckDelaunay(p2, p3, p0, p1))
                    {
                        continue;
                    }
                }
                else// if(p3 == ne)
                {
                    if (CheckDelaunay(p0, p1, p2, p3))
                    {
                        continue;
                    }
                }
                // 辺をフリップ。
                {
                    edges[ee].p0 = p1;
                    edges[ee].p1 = p3;
                    if (edges[e0].t0 == t0)
                    {
                        edges[e0].t0 = t1;
                    }
                    if (edges[e0].t1 == t0)
                    {
                        edges[e0].t1 = t1;
                    }
                    if (edges[e2].t0 == t1)
                    {
                        edges[e2].t0 = t0;
                    }
                    if (edges[e2].t1 == t1)
                    {
                        edges[e2].t1 = t0;
                    }
                    SetTriangle(t0, p3, p1, p2, ee, e1, e2);
                    SetTriangle(t1, p1, p3, p0, ee, e3, e0);
                }
                // 追加した頂点の反対にあった三角形の辺を検査登録。
                if (p1 == px)
                {
                    edgeStack.Push(e2);
                    edgeStack.Push(e3);
                }
                else/* if(p3 == ne)*/
                {
                    edgeStack.Push(e0);
                    edgeStack.Push(e1);
                }
            }
            while (edgeStack.Count != 0);
        }
        /// <summary>
        /// ドローネメッシュに頂点追加
        /// 最初に４頂点を追加し以降は領域内でメッシュを分割
        /// 領域外の頂点が指定されると-1を返す、成功時は頂点番号
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Insert(float x, float y)
        {
            // 頂点を追加
            var px = AddPoint(x, y);

            if (numPoints < 4)
            {
                // 最初四点で領域を指定するので、そろうまでは頂点登録のみ。
                return px;
            }
            if (numPoints == 4)
            {
                // ４頂点そろったら領域を構築、４点目が領域に含まれるなら通常処理へ。
                if (!Setup())
                {
                    return px;
                }
            }
            {
                // 頂点が含まれる三角形を見つける
                var tt = FindTriangle(px);
                if (tt < 0)
                {
                    // 範囲外
                    return -1;
                }

                // 三角形を分割する
                DivideTrinangle(px, tt);

                // パタパタ
                FlipFlop(px);

                return px;
            }
        }
    }
}
