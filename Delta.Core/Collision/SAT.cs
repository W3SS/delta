﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Delta.Collision.Geometry;
using Microsoft.Xna.Framework;

namespace Delta.Collision
{

    public struct CollisionResult
    {
        public Polygon Us;
        public Polygon Them;

        /// <summary>
        /// The polygons interesct each other.
        /// </summary>
        public bool IsColliding;

        /// <summary>
        /// The minimum translation vector to seperate the two poloygons.
        /// </summary>
        public Vector2 CollisionResponse;
    }

    /// <summary>
    /// SAT, or Seperating Axis Theorem provides narrow phase collision detection. Ie. It checks if polygons
    /// are intersecting each other (colliding) and what the proper response is to resolve the collision.
    /// This is done by checking all potential seperating axisis for seperating lines. ie. For a space
    /// in their projected size. If a seperating axis exists, then we know the objects are not intersecting
    /// and are provided with an early out.
    /// </summary>
    public static class SAT
    {
        public static CollisionResult NoCollision = new CollisionResult() { IsColliding = false };

        public static CollisionResult BoxBox(OBB a, OBB b)
        {
            float projectedDistance = 0;
            float minPenetration = float.MaxValue;
            Vector2 distance = a.Position - b.Position;
            Vector2 mtv = default(Vector2); // the minimum translation vector

            // merge normals from both polygons
            // NOTE: For OBB's we only need to check their half widths. ie. 4 axis total.
            // For AABB's we only need to check 2 axis since they don't rotate.
            Vector2[] axisToCheck = new Vector2[] {
                a.Orientation,
                a.PerpOrientation,
                b.Orientation,
                b.PerpOrientation
            };

            // TODO: remove parallel normals

            for (int i = 0; i < axisToCheck.Length; i++)
            {
                Vector2 projectionA, projectionB;
                projectedDistance = Math.Abs(Vector2.Dot(distance, axisToCheck[i]));
                a.ProjectOntoAxis(ref axisToCheck[i], out projectionA);
                b.ProjectOntoAxis(ref axisToCheck[i], out projectionB);
                float aSize = Math.Abs(projectionA.X) + Math.Abs(projectionA.Y);
                float bSize = Math.Abs(projectionB.X) + Math.Abs(projectionB.Y);
                float abSize = aSize + bSize;
                float penetration = abSize - projectedDistance;

                // a seperating axis found; there is no collision.
                if (penetration <= 0)
                {
                    return NoCollision;
                }
                // project the object along the axis with the smalled penetration depth.
                else if (Math.Abs(penetration) < Math.Abs(minPenetration))
                {
                    minPenetration = penetration;
                    mtv = axisToCheck[i];
                }
            }
            // the distance vector determines the direction of movement. the distance and mtv
            // should always oppose each other to repel collisions.
            if (Vector2.Dot(distance, mtv) < 0f)
                mtv = -mtv;
            // seperating axis could not be found; a collision occurs.
            return new CollisionResult()
            {
                Us = a,
                Them = b,
                CollisionResponse = minPenetration * mtv,
                IsColliding = true
            };
        }

        public static CollisionResult CircleCircle(Circle circleA, Circle circleB)
        {
            Vector2 distance = circleA.Position - circleB.Position;
            float abSize = circleA.Radius + circleB.Radius;
            float penetration = abSize - distance.Length();
            Vector2 mtv = default(Vector2);
            if (penetration <= 0)
            {
                return NoCollision;
            }
            mtv = distance;
            Vector2Extensions.SafeNormalize(ref mtv);
            // the distance vector determines the direction of movement. the distance and mtv
            // should always oppose each other to repel collisions.
            if (Vector2.Dot(distance, mtv) < 0f)
                mtv = -mtv;
            return new CollisionResult()
            {
                Us = circleA,
                Them = circleB,
                CollisionResponse = penetration * mtv,
                IsColliding = true
            };
        }

        public static CollisionResult CircleBox(Circle circleA, OBB boxB)
        {
            float projectedDistance = 0;
            float minPenetration = float.MaxValue;
            Vector2 distance = circleA.Position - boxB.Position;
            Vector2 mtv = default(Vector2); // the minimum translation vector

            Vector2[] axisToCheck = new Vector2[3];
            axisToCheck[0] = boxB.Orientation;
            axisToCheck[1] = boxB.PerpOrientation;
            float minDistance = float.MaxValue;
            Vector2 closestVertex = default(Vector2);
            for (int i = 0; i < boxB.Vertices.Length; i++)
            {
                float vertexDistance = Vector2.DistanceSquared(circleA.Position, boxB.Vertices[i]);
                if (vertexDistance < minDistance)
                {
                    minDistance = vertexDistance;
                    closestVertex = circleA.Position - boxB.Vertices[i];
                }
            }
            axisToCheck[2] = closestVertex;
            Vector2Extensions.SafeNormalize(ref axisToCheck[2]);

            for (int i = 0; i < axisToCheck.Length; i++)
            {
                Vector2 projectionA, projectionB;

                projectedDistance = Math.Abs(Vector2.Dot(distance, axisToCheck[i]));
                circleA.ProjectOntoAxis(ref axisToCheck[i], out projectionA);
                boxB.ProjectOntoAxis(ref axisToCheck[i], out projectionB);

                float aSize = projectionA.Length(); //Math.Abs(projectionA.X) + Math.Abs(projectionA.Y);
                float bSize = Math.Abs(projectionB.X) + Math.Abs(projectionB.Y);
                float abSize = aSize + bSize;
                float penetration = abSize - projectedDistance;

                // a seperating axis found; there is no collision.
                if (penetration <= 0)
                {
                    return NoCollision;
                }
                // project the object along the axis with the smalled penetration depth.
                else if (Math.Abs(penetration) < Math.Abs(minPenetration))
                {
                    minPenetration = penetration;
                    mtv = axisToCheck[i];
                }
            }
            // the distance vector determines the direction of movement. the distance and mtv
            // should always oppose each other to repel collisions.
            if (Vector2.Dot(distance, mtv) < 0f)
                mtv = -mtv;
            // seperating axis could not be found; a collision occurs.
            return new CollisionResult()
            {
                Us = circleA,
                Them = boxB,
                CollisionResponse = minPenetration * mtv,
                IsColliding = true
            };
        }

        public static CollisionResult PolyPoly(Polygon a, Polygon b)
        {
            float projectedDistance = 0;
            float minPenetration = float.MaxValue;
            Vector2 distance = a.Position - b.Position;
            Vector2 mtv = default(Vector2); // the minimum translation vector

            // merge normals from both polygons
            Vector2[] axisToCheck = new Vector2[a.Normals.Length + b.Normals.Length];
            for (int i = 0; i < a.Normals.Length; i++)
                axisToCheck[i] = a.Normals[i];
            for (int i = a.Normals.Length; i < axisToCheck.Length; i++)
                axisToCheck[i] = b.Normals[i - a.Normals.Length];

            // TODO: remove parallel normals

            for (int i = 0; i < axisToCheck.Length; i++)
            {
                float minA, maxA, minB, maxB;
                minA = maxA = minB = maxB = 0;

                projectedDistance = Math.Abs(Vector2.Dot(distance, axisToCheck[i]));
                a.ProjectOntoAxis(ref axisToCheck[i], out minA, out maxA);
                b.ProjectOntoAxis(ref axisToCheck[i], out minB, out maxB);
                float penetration = maxB - minA;

                // a seperating axis has been found; there is no collision.
                if (minA - maxB > 0f || minB - maxA > 0f)
                {
                    return NoCollision;
                }
                // project the object along the axis with the smalled penetration depth.
                else if (Math.Abs(penetration) < Math.Abs(minPenetration))
                {
                    minPenetration = penetration;
                    mtv = axisToCheck[i];
                }
            }
            // the distance vector determines the direction of movement. the distance and mtv
            // should always oppose each other to repel collisions.
            if (Vector2.Dot(distance, mtv) < 0f)
                mtv = -mtv;
            // seperating axis could not be found; a collision occurs.
            return new CollisionResult()
            {
                Us = a,
                Them = b,
                CollisionResponse = minPenetration * mtv,
                IsColliding = true
            };
        } 
    }
}
