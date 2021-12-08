using UnityEditor;
using UnityEngine;

namespace IrisFenrir.Graphic
{
    public class Bezier
    {
        // 三次贝塞尔曲线的四个点
        public Vector3 P0 { get; set; }
        public Vector3 P1 { get; set; }
        public Vector3 P2 { get; set; }
        public Vector3 P3 { get; set; }

        public Color LineColor { get; set; } // 曲线颜色
        public float LineWidth { get; set; } // 曲线宽度

        public Bezier() { }
        public Bezier(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        // 绘制曲线
        public void Draw()
        {
            Handles.DrawBezier(P0, P3, P1, P2, LineColor, null, LineWidth);
        }
        // 获取曲线上一点的位置
        public Vector3 GetPoint(float t)
        {
            return P0 * (1 - t) * (1 - t) * (1 - t) +
                3 * P1 * t * (1 - t) * (1 - t) +
                3 * P2 * t * t * (1 - t) +
                P3 * t * t * t;
        }

        // 获取曲线上一点的切线方向
        public Vector3 TanDir(float t)
        {
            return -3 * P0 * (1 - t) * (1 - t) +
                3 * P1 * (1 - t) * (1 - 3 * t) +
                3 * P2 * t * (2 - 3 * t) +
                3 * P2 * t * t;
        }

        // 获取曲线上一点的切线角度(0-360)
        public float Angle(float t)
        {
            var dir = TanDir(t);
            if(dir.y >= 0)
            {
                return Vector3.Angle(Vector3.right, TanDir(t));
            }
            else
            {
                return -Vector3.Angle(Vector3.right, TanDir(t));
            }
        }
    }
}
