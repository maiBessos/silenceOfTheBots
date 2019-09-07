using GoE.GameLogic;
using GoE.GameLogic.EvolutionaryStrategy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoE.Utils
{
    namespace Algorithms
    {
        public struct Segment
        {
            public Point start { get; set; }
            public Point end { get; set; }

            public int manLen()
            {
                return start.manDist(end);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="P"></param>
            /// <returns>
            /// 0 to 1, where 0 means closest point is start, and 1 means end
            /// </returns>
            public float GetClosestPointOnLineSegmentFactor(Point P, out Point closestPoint)
            {
                Point AP = P.subtruct(start);       //Vector from A to P   
                Point AB = end.subtruct(start);       //Vector from A to B  

                float magnitudeAB = AB.lenSqr();     //Magnitude of AB vector (it's length squared)     
                float ABAPproduct = AP.dotProduct(AB);    //The DOT product of a_to_p and a_to_b     
                float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

                if (distance < 0)     //Check if P projection is over vectorAB     
                {
                    closestPoint = start;
                    return 0;
                }
                else if (distance > 1)
                {
                    closestPoint = end;
                    return 1;
                }
                // return start + dist * AB, but rounded to match the closest discrete point
                closestPoint = start.add((int)Math.Round(AB.X * distance), (int)Math.Round(AB.Y * distance));
                return distance;

            }
            public Point getPoint(float factor)
            {
                Point AB = end.subtruct(start);
                return start.add((int)Math.Round(AB.X * factor), (int)Math.Round(AB.Y * factor));
            }

        }

        public struct SegmentF
        {
            public override string ToString()
            {
                return "(" + start.X.ToString() + "," + start.Y.ToString() + ")->("+
                        end.X.ToString() + "," + end.Y.ToString() + ")";
            }
            public static SegmentF makeSeg(PointF Start, PointF End)
            {
                return new SegmentF() { start = Start, end = End };
            }
            public static SegmentF makeSeg(float x1,float y1,float x2,float y2)
            {
                return new SegmentF() { start = new PointF(x1, y1), end = new PointF(x2, y2) };
            }
            public PointF start { get; set; }
            public PointF end { get; set; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="P"></param>
            /// <returns>
            /// if between 0 and 1, then closest point is on segment
            /// </returns>
            public float GetProjectionOnLine(PointF P)
            {
                PointF AP = P.subtruct(start);       //Vector from A to P   
                PointF AB = end.subtruct(start);       //Vector from A to B  

                float magnitudeAB = AB.lenSqr();     //Magnitude of AB vector (it's length squared)     
                float ABAPproduct = AP.dotProduct(AB);    //The DOT product of a_to_p and a_to_b     
                return ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="P"></param>
            /// <returns>
            /// 0 to 1, where 0 means closest point is start, and 1 means end
            /// </returns>
            public float GetClosestPointOnLineSegmentFactor(PointF P, out PointF closestPoint)
            {
                PointF AP = P.subtruct(start);       //Vector from A to P   
                PointF AB = end.subtruct(start);       //Vector from A to B  

                float magnitudeAB = AB.lenSqr();     //Magnitude of AB vector (it's length squared)     
                float ABAPproduct = AP.dotProduct(AB);    //The DOT product of a_to_p and a_to_b     
                float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

                if (distance < 0)     //Check if P projection is over vectorAB     
                {
                    closestPoint = start;
                    return 0;
                }
                else if (distance > 1)
                {
                    closestPoint = end;
                    return 1;
                }
                // return start + dist * AB, but rounded to match the closest discrete point
                closestPoint = start.addF(AB.X * distance, AB.Y * distance);
                return distance;

            }
            public PointF getPoint(float factor)
            {
                PointF AB = end.subtruct(start);
                return start.addF(AB.X * factor, AB.Y * factor);
            }
          
            public float distance(PointF p)
            {
                float ds = p.distance(start);
                float de = p.distance(end);

                PointF closest;
                GetClosestPointOnLineSegmentFactor(p, out closest);
                float dn = closest.distance(p);
                return Math.Min(Math.Min(ds, de), dn);
            }
            
        }
        public struct LineF
        {
            public float a, b; // a may be float.PositiveInfinity
            //public PointF p1, p2;

            public static LineF makeLine(PointF p1, PointF p2)
            {
                var l = new LineF();
                //l.p1 = p1;
                //l.p2 = p2;
                if(p1.X == p2.X)
                {
                    l.a = float.PositiveInfinity;
                    l.b = p1.X;
                    return l;
                }
                l.a = (p2.Y - p1.Y) / (p2.X - p1.X);
                l.b = p1.Y - l.a * p1.X;
                return l;
            }

            public float getY(float x)
            {
                return a * x + b;
            }
        }
        class LineIntersection
        {
            public static bool LineIntersectsRectTest(LineF line, RectangleF r)
            {
                if (line.a == float.PositiveInfinity)
                    return r.Left <= line.b && r.Right >= line.b;

                var vert = r.getVertices();
                bool areBelow = vert[0].Y < line.getY(vert[0].X);

                for (int i = 1; i < 4; ++i)
                    if (areBelow != (vert[i].Y < line.getY(vert[i].X))) // some points in rect are below the line, some are above
                        return true;
                return false;
            }

            /// <returns>
            /// false - no intersection, or infinite intersecion
            /// true - single intersection point
            /// </returns>
            public static bool LineIntersectsLine(LineF l1, LineF l2, ref PointF intersec)
            {
                if (l1.a == l2.a)
                    return false;

                if (float.IsInfinity(l1.a))
                {
                    intersec = new PointF(l1.b, l2.getY(l1.b));
                    return true;
                }
                if (float.IsInfinity(l2.a))
                {
                    intersec = new PointF(l2.b, l1.getY(l2.b));
                    return true;
                }

                float x = (l2.b - l1.b) / (l1.a - l2.a); // the solution for the equation between two lines
                intersec = new PointF(x, l1.getY(x));
                return true;
            }
            
            /// <returns>
            /// false - no intersection, or infinite intersecion
            /// true - single intersection point
            /// </returns>
            public static bool LineIntersectsSgment(LineF line, SegmentF seg, ref PointF intersec)
            {
                var segLine = LineF.makeLine(seg.start, seg.end);
                if (LineIntersectsLine(line, segLine, ref intersec))
                {
                    float p = seg.GetProjectionOnLine(intersec);
                    return p >= 0 && p <= 1;
                }
                return false;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="line"></param>
            /// <param name="r"></param>
            /// <param name="intersects">
            /// if return value is true, intersects will have two intersection points (even for infinite case)
            /// </param>
            /// <returns>
            /// false - no intersection
            /// true - single/ infinite intersection
            /// </returns>
            public static bool LineIntersectsRect(LineF line, RectangleF r, out PointF[] intersects)
            {
                intersects = new PointF[2];
                int intersecIdx = 0;
                var verts = r.getVertices();
                // check intersection with each of the rect's segments
                for (int i = 0; i < 4 && intersecIdx < 2; ++i)
                    if (LineIntersectsSgment(line, 
                                             new SegmentF() { start = verts[i], end = verts[(i + 1) % 4] }, 
                                             ref intersects[intersecIdx]))
                    {
                        ++intersecIdx; // if 2 intersections were found, the loop breaks
                    }

                return intersecIdx == 2;
            }
            
            

        }
    }
}
