using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

#nullable disable
namespace AetherBox.Debugging
{
    public static class Doom
    {
        private static readonly double[] Cam = new double[2]
    {
      7.7,
      1.8
    };
        private static double Dir = -1.0 * Math.PI / 2.0;
        private static double[][] Enemies = new double[3][]
    {
      new double[2]{ 29.0, -27.0 },
      new double[2]{ 35.0, -19.0 },
      new double[2]{ 24.0, -22.0 }
    };
        private const double FloatingPointEqualTolerance = 1E-07;
        private static readonly Random Random = new Random();

        public static double GetSliderValue(float t, float x, int i)
        {
            IKeyState keyState = Svc.KeyState;
            if (i == 0)
            {
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 5);
                interpolatedStringHandler.AppendFormatted<bool>(keyState[37]);
                interpolatedStringHandler.AppendLiteral(", ");
                interpolatedStringHandler.AppendFormatted<bool>(keyState[39]);
                interpolatedStringHandler.AppendLiteral(", ");
                interpolatedStringHandler.AppendFormatted<bool>(keyState[38]);
                interpolatedStringHandler.AppendLiteral(", ");
                interpolatedStringHandler.AppendFormatted<bool>(keyState[40]);
                interpolatedStringHandler.AppendLiteral(", ");
                interpolatedStringHandler.AppendFormatted<bool>(keyState[32]);
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                if (keyState[37])
                    Doom.Dir -= 0.04;
                if (keyState[39])
                    Doom.Dir += 0.04;
                if (keyState[38])
                {
                    Doom.Cam[0] += 0.2 * Math.Cos(Doom.Dir);
                    Doom.Cam[1] += 0.2 * Math.Sin(Doom.Dir);
                }
                if (keyState[40])
                {
                    Doom.Cam[0] -= 0.2 * Math.Cos(Doom.Dir);
                    Doom.Cam[1] -= 0.2 * Math.Sin(Doom.Dir);
                }
                for (int index = 0; index < Doom.Enemies.Length; ++index)
                {
                    double[] enemy = Doom.Enemies[index];
                    if (Math.Sqrt(Math.Pow(enemy[0] - Doom.Cam[0], 2.0) + Math.Pow(enemy[1] - Doom.Cam[1], 2.0)) < 10.0)
                    {
                        double num = Math.Atan2(enemy[1] - Doom.Cam[1], enemy[0] - Doom.Cam[0]);
                        enemy[0] -= 0.03 * Math.Cos(num);
                        enemy[1] -= 0.03 * Math.Sin(num);
                    }
                }
            }
            double d = Math.Atan(((double) x - 0.5) * 1.5);
            double num1 = Doom.Dir + d;
            double[] end = new double[2]
      {
        Doom.Cam[0] + Math.Cos(num1) * 999.0,
        Doom.Cam[1] + Math.Sin(num1) * 999.0
      };
            if ((i == 32 || i == 31) && keyState[32])
            {
                for (int index = 0; index < Doom.Enemies.Length; ++index)
                {
                    double[] enemy = Doom.Enemies[index];
                    double[] numArray = intersect2(Doom.Cam[0], Doom.Cam[1], end[0], end[1], enemy[0], enemy[1], 0.3);
                    if (numArray != null && numArray.Length > 0)
                    {
                        List<double[]> list = ((IEnumerable<double[]>) Doom.Enemies).ToList<double[]>();
                        list.RemoveAt(index);
                        Doom.Enemies = list.ToArray();
                    }
                }
            }
            if (keyState[32])
            {
                switch (i)
                {
                    case 29:
                    case 34:
                        return 0.05;
                    case 31:
                    case 32:
                        return 0.2;
                }
            }
            List<double[][]> walls = new List<double[][]>()
      {
        new double[2][]{ new double[2], new double[2]{ 2.8, 0.0 } },
        new double[2][]
        {
          new double[2]{ 2.8, 0.0 },
          new double[2]{ 5.6, 2.1 }
        },
        new double[2][]
        {
          new double[2]{ 5.6, 2.1 },
          new double[2]{ 8.4, 2.1 }
        },
        new double[2][]
        {
          new double[2]{ 8.4, 2.1 },
          new double[2]{ 9.1, 2.1 }
        },
        new double[2][]
        {
          new double[2]{ 9.1, 2.1 },
          new double[2]{ 12.5, 0.0 }
        },
        new double[2][]
        {
          new double[2]{ 12.5, 0.0 },
          new double[2]{ 14.0, 0.0 }
        },
        new double[2][]
        {
          new double[2]{ 14.0, 0.0 },
          new double[2]{ 14.0, -14.3 }
        },
        new double[2][]
        {
          new double[2]{ 14.0, -14.3 },
          new double[2]{ 14.9, -20.7 }
        },
        new double[2][]
        {
          new double[2]{ 14.9, -20.7 },
          new double[2]{ 16.7, -21.5 }
        },
        new double[2][]
        {
          new double[2]{ 16.7, -21.5 },
          new double[2]{ 20.8, -21.5 }
        },
        new double[2][]
        {
          new double[2]{ 20.8, -21.5 },
          new double[2]{ 21.1, -15.8 }
        },
        new double[2][]
        {
          new double[2]{ 21.1, -15.8 },
          new double[2]{ 38.7, -15.8 }
        },
        new double[2][]
        {
          new double[2]{ 38.7, -15.8 },
          new double[2]{ 38.7, -32.0 }
        },
        new double[2][]
        {
          new double[2]{ 38.7, -32.0 },
          new double[2]{ 21.0, -32.0 }
        },
        new double[2][]
        {
          new double[2]{ 21.0, -32.0 },
          new double[2]{ 21.0, -24.2 }
        },
        new double[2][]
        {
          new double[2]{ 21.0, -24.2 },
          new double[2]{ 16.8, -24.0 }
        },
        new double[2][]
        {
          new double[2]{ 16.8, -24.0 },
          new double[2]{ 11.9, -22.0 }
        },
        new double[2][]
        {
          new double[2]{ 11.9, -22.0 },
          new double[2]{ 11.1, -14.5 }
        },
        new double[2][]
        {
          new double[2]{ 11.1, -14.5 },
          new double[2]{ 5.7, -14.4 }
        },
        new double[2][]
        {
          new double[2]{ 5.7, -14.4 },
          new double[2]{ 2.9, -13.1 }
        },
        new double[2][]
        {
          new double[2]{ 2.9, -13.1 },
          new double[2]{ 0.0, -13.0 }
        },
        new double[2][]
        {
          new double[2]{ 0.0, -13.0 },
          new double[2]{ 0.0, -9.7 }
        },
        new double[2][]
        {
          new double[2]{ 0.0, -9.7 },
          new double[2]{ -4.2, -8.7 }
        },
        new double[2][]
        {
          new double[2]{ -4.2, -8.7 },
          new double[2]{ -5.3, -11.5 }
        },
        new double[2][]
        {
          new double[2]{ -5.3, -11.5 },
          new double[2]{ -12.3, -11.5 }
        },
        new double[2][]
        {
          new double[2]{ -12.3, -11.5 },
          new double[2]{ -12.3, -2.1 }
        },
        new double[2][]
        {
          new double[2]{ -12.3, -2.1 },
          new double[2]{ -5.4, -2.1 }
        },
        new double[2][]
        {
          new double[2]{ -5.4, -2.1 },
          new double[2]{ -4.2, -4.9 }
        },
        new double[2][]
        {
          new double[2]{ -4.2, -4.9 },
          new double[2]{ 0.0, -4.0 }
        },
        new double[2][]{ new double[2]{ 0.0, -4.0 }, new double[2] }
      };
            AddColumn(4.6, -3.7);
            AddColumn(10.8, -3.7);
            AddColumn(10.8, -10.0);
            AddColumn(4.6, -10.0);
            IEnumerable<double> source1 = walls.Select<double[][], double[]>((Func<double[][], double[]>) (w => intersect(Doom.Cam[0], Doom.Cam[1], end[0], end[1], w[0][0], w[0][1], w[1][0], w[1][1]))).Where<double[]>((Func<double[], bool>) (p => p != null)).Select<double[], double>((Func<double[], double>) (p => Math.Sqrt(Math.Pow(p[0] - Doom.Cam[0], 2.0) + Math.Pow(p[1] - Doom.Cam[1], 2.0))));
            double num2 = source1.Any<double>() ? source1.Min() : 9999.0;
            IEnumerable<double> source2 = ((IEnumerable<double[]>) Doom.Enemies).Select<double[], double[]>((Func<double[], double[]>) (e => intersect2(Doom.Cam[0], Doom.Cam[1], end[0], end[1], e[0], e[1], 0.3))).Where<double[]>((Func<double[], bool>) (p => p != null && p.Length != 0)).Select<double[], double>((Func<double[], double>) (p => Math.Sqrt(Math.Pow(p[0] - Doom.Cam[0], 2.0) + Math.Pow(p[1] - Doom.Cam[1], 2.0))));
            double num3 = source2.Any<double>() ? source2.Min() : 9999.0;
            return num2 < num3 ? 0.5 + 1.0 / (Math.Cos(d) * num2) * 2.0 * ((double)(i % 2) - 0.5) : 0.5 + 1.0 / (Math.Cos(d) * num3) * 1.0 * ((double)(i % 2) - 0.75) - Doom.Random.NextDouble() * 0.1 * (1.0 / num3);

            static double[] intersect(
              double x1,
              double y1,
              double x2,
              double y2,
              double x3,
              double y3,
              double x4,
              double y4)
            {
                if (Math.Abs(x1 - x2) < 1E-07 && Math.Abs(y1 - y2) < 1E-07 || Math.Abs(x3 - x4) < 1E-07 && Math.Abs(y3 - y4) < 1E-07)
                    return (double[])null;
                double num1 = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
                if (num1 == 0.0)
                    return (double[])null;
                double num2 = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / num1;
                double num3 = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / num1;
                if (num2 < 0.0 || num2 > 1.0 || num3 < 0.0 || num3 > 1.0)
                    return (double[])null;
                return new double[2]
                {
          x1 + num2 * (x2 - x1),
          y1 + num2 * (y2 - y1)
                };
            }

            static double[] intersect2(
              double x1,
              double y1,
              double x2,
              double y2,
              double cx,
              double cy,
              double r)
            {
                double num1 = x2 - x1;
                double num2 = y2 - y1;
                double num3 = x1 - cx;
                double num4 = y1 - cy;
                double num5 = -2.0 * (num1 * num3 + num2 * num4);
                double num6 = 2.0 * (num1 * num1 + num2 * num2);
                double d = Math.Sqrt(num5 * num5 - 2.0 * num6 * (num3 * num3 + num4 * num4 - r * r));
                if (double.IsNaN(d))
                    return (double[])null;
                double num7 = (num5 - d) / num6;
                if (num7 > 1.0 || num7 < 0.0)
                    return (double[])null;
                return new double[2]
                {
          x1 + num1 * num7,
          y1 + num2 * num7
                };
            }

            void AddColumn(double cx, double cy)
            {
                double num = 0.3;
                walls.Add(new double[2][]
                {
          new double[2]{ cx - num, cy - num },
          new double[2]{ cx - num, cy + num }
                });
                walls.Add(new double[2][]
                {
          new double[2]{ cx - num, cy + num },
          new double[2]{ cx + num, cy + num }
                });
                walls.Add(new double[2][]
                {
          new double[2]{ cx + num, cy + num },
          new double[2]{ cx + num, cy - num }
                });
                walls.Add(new double[2][]
                {
          new double[2]{ cx + num, cy - num },
          new double[2]{ cx - num, cy - num }
                });
            }
        }
    }
}
