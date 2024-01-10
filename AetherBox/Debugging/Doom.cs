using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ImGuiNET;

namespace AetherBox.Debugging;

public static class Doom
{
	private static readonly double[] Cam = new double[2] { 7.7, 1.8 };

	private static double Dir = -Math.PI / 2.0;

	private static double[][] Enemies = new double[3][]
	{
		new double[2] { 29.0, -27.0 },
		new double[2] { 35.0, -19.0 },
		new double[2] { 24.0, -22.0 }
	};

	private const double FloatingPointEqualTolerance = 1E-07;

	private static readonly Random Random = new Random();

	public static double GetSliderValue(float t, float x, int i)
	{
		IKeyState pressed = Svc.KeyState;
		if (i == 0)
		{
			ImGui.Text($"{pressed[37]}, {pressed[39]}, {pressed[38]}, {pressed[40]}, {pressed[32]}");
			if (pressed[37])
			{
				Dir -= 0.04;
			}
			if (pressed[39])
			{
				Dir += 0.04;
			}
			if (pressed[38])
			{
				Cam[0] += 0.2 * Math.Cos(Dir);
				Cam[1] += 0.2 * Math.Sin(Dir);
			}
			if (pressed[40])
			{
				Cam[0] -= 0.2 * Math.Cos(Dir);
				Cam[1] -= 0.2 * Math.Sin(Dir);
			}
			for (int eIndex = 0; eIndex < Enemies.Length; eIndex++)
			{
				double[] e3 = Enemies[eIndex];
				if (Math.Sqrt(Math.Pow(e3[0] - Cam[0], 2.0) + Math.Pow(e3[1] - Cam[1], 2.0)) < 10.0)
				{
					double a2 = Math.Atan2(e3[1] - Cam[1], e3[0] - Cam[0]);
					e3[0] -= 0.03 * Math.Cos(a2);
					e3[1] -= 0.03 * Math.Sin(a2);
				}
			}
		}
		double aa = Math.Atan(((double)x - 0.5) * 1.5);
		double a = Dir + aa;
		double[] end = new double[2]
		{
			Cam[0] + Math.Cos(a) * 999.0,
			Cam[1] + Math.Sin(a) * 999.0
		};
		if ((i == 32 || i == 31) && pressed[32])
		{
			for (int index = 0; index < Enemies.Length; index++)
			{
				double[] e2 = Enemies[index];
				double[] inter = intersect2(Cam[0], Cam[1], end[0], end[1], e2[0], e2[1], 0.3);
				if (inter != null && inter.Length > 0)
				{
					List<double[]> list = Enemies.ToList();
					list.RemoveAt(index);
					Enemies = list.ToArray();
				}
			}
		}
		if (pressed[32])
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
		List<double[][]> walls = new List<double[][]>
		{
			new double[2][]
			{
				new double[2],
				new double[2] { 2.8, 0.0 }
			},
			new double[2][]
			{
				new double[2] { 2.8, 0.0 },
				new double[2] { 5.6, 2.1 }
			},
			new double[2][]
			{
				new double[2] { 5.6, 2.1 },
				new double[2] { 8.4, 2.1 }
			},
			new double[2][]
			{
				new double[2] { 8.4, 2.1 },
				new double[2] { 9.1, 2.1 }
			},
			new double[2][]
			{
				new double[2] { 9.1, 2.1 },
				new double[2] { 12.5, 0.0 }
			},
			new double[2][]
			{
				new double[2] { 12.5, 0.0 },
				new double[2] { 14.0, 0.0 }
			},
			new double[2][]
			{
				new double[2] { 14.0, 0.0 },
				new double[2] { 14.0, -14.3 }
			},
			new double[2][]
			{
				new double[2] { 14.0, -14.3 },
				new double[2] { 14.9, -20.7 }
			},
			new double[2][]
			{
				new double[2] { 14.9, -20.7 },
				new double[2] { 16.7, -21.5 }
			},
			new double[2][]
			{
				new double[2] { 16.7, -21.5 },
				new double[2] { 20.8, -21.5 }
			},
			new double[2][]
			{
				new double[2] { 20.8, -21.5 },
				new double[2] { 21.1, -15.8 }
			},
			new double[2][]
			{
				new double[2] { 21.1, -15.8 },
				new double[2] { 38.7, -15.8 }
			},
			new double[2][]
			{
				new double[2] { 38.7, -15.8 },
				new double[2] { 38.7, -32.0 }
			},
			new double[2][]
			{
				new double[2] { 38.7, -32.0 },
				new double[2] { 21.0, -32.0 }
			},
			new double[2][]
			{
				new double[2] { 21.0, -32.0 },
				new double[2] { 21.0, -24.2 }
			},
			new double[2][]
			{
				new double[2] { 21.0, -24.2 },
				new double[2] { 16.8, -24.0 }
			},
			new double[2][]
			{
				new double[2] { 16.8, -24.0 },
				new double[2] { 11.9, -22.0 }
			},
			new double[2][]
			{
				new double[2] { 11.9, -22.0 },
				new double[2] { 11.1, -14.5 }
			},
			new double[2][]
			{
				new double[2] { 11.1, -14.5 },
				new double[2] { 5.7, -14.4 }
			},
			new double[2][]
			{
				new double[2] { 5.7, -14.4 },
				new double[2] { 2.9, -13.1 }
			},
			new double[2][]
			{
				new double[2] { 2.9, -13.1 },
				new double[2] { 0.0, -13.0 }
			},
			new double[2][]
			{
				new double[2] { 0.0, -13.0 },
				new double[2] { 0.0, -9.7 }
			},
			new double[2][]
			{
				new double[2] { 0.0, -9.7 },
				new double[2] { -4.2, -8.7 }
			},
			new double[2][]
			{
				new double[2] { -4.2, -8.7 },
				new double[2] { -5.3, -11.5 }
			},
			new double[2][]
			{
				new double[2] { -5.3, -11.5 },
				new double[2] { -12.3, -11.5 }
			},
			new double[2][]
			{
				new double[2] { -12.3, -11.5 },
				new double[2] { -12.3, -2.1 }
			},
			new double[2][]
			{
				new double[2] { -12.3, -2.1 },
				new double[2] { -5.4, -2.1 }
			},
			new double[2][]
			{
				new double[2] { -5.4, -2.1 },
				new double[2] { -4.2, -4.9 }
			},
			new double[2][]
			{
				new double[2] { -4.2, -4.9 },
				new double[2] { 0.0, -4.0 }
			},
			new double[2][]
			{
				new double[2] { 0.0, -4.0 },
				new double[2]
			}
		};
		AddColumn(4.6, -3.7);
		AddColumn(10.8, -3.7);
		AddColumn(10.8, -10.0);
		AddColumn(4.6, -10.0);
		IEnumerable<double> xs = from w in walls
			select intersect(Cam[0], Cam[1], end[0], end[1], w[0][0], w[0][1], w[1][0], w[1][1]) into p
			where p != null
			select Math.Sqrt(Math.Pow(p[0] - Cam[0], 2.0) + Math.Pow(p[1] - Cam[1], 2.0));
		double nearestWall = (xs.Any() ? xs.Min() : 9999.0);
		IEnumerable<double> xs2 = from e in Enemies
			select intersect2(Cam[0], Cam[1], end[0], end[1], e[0], e[1], 0.3) into p
			where p != null && p.Length != 0
			select Math.Sqrt(Math.Pow(p[0] - Cam[0], 2.0) + Math.Pow(p[1] - Cam[1], 2.0));
		double nearestEnemy = (xs2.Any() ? xs2.Min() : 9999.0);
		if (nearestWall < nearestEnemy)
		{
			return 0.5 + 1.0 / (Math.Cos(aa) * nearestWall) * 2.0 * ((double)(i % 2) - 0.5);
		}
		return 0.5 + 1.0 / (Math.Cos(aa) * nearestEnemy) * 1.0 * ((double)(i % 2) - 0.75) - Random.NextDouble() * 0.1 * (1.0 / nearestEnemy);
		void AddColumn(double cx, double cy)
		{
			double cr = 0.3;
			walls.Add(new double[2][]
			{
				new double[2]
				{
					cx - cr,
					cy - cr
				},
				new double[2]
				{
					cx - cr,
					cy + cr
				}
			});
			walls.Add(new double[2][]
			{
				new double[2]
				{
					cx - cr,
					cy + cr
				},
				new double[2]
				{
					cx + cr,
					cy + cr
				}
			});
			walls.Add(new double[2][]
			{
				new double[2]
				{
					cx + cr,
					cy + cr
				},
				new double[2]
				{
					cx + cr,
					cy - cr
				}
			});
			walls.Add(new double[2][]
			{
				new double[2]
				{
					cx + cr,
					cy - cr
				},
				new double[2]
				{
					cx - cr,
					cy - cr
				}
			});
		}
		static double[] intersect(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
		{
			if ((Math.Abs(x1 - x2) < 1E-07 && Math.Abs(y1 - y2) < 1E-07) || (Math.Abs(x3 - x4) < 1E-07 && Math.Abs(y3 - y4) < 1E-07))
			{
				return null;
			}
			double d = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
			if (d == 0.0)
			{
				return null;
			}
			double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / d;
			double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / d;
			if (!(ua < 0.0) && !(ua > 1.0) && !(ub < 0.0) && !(ub > 1.0))
			{
				return new double[2]
				{
					x1 + ua * (x2 - x1),
					y1 + ua * (y2 - y1)
				};
			}
			return null;
		}
		static double[] intersect2(double x1, double y1, double x2, double y2, double cx, double cy, double r)
		{
			double v1x = x2 - x1;
			double v1y = y2 - y1;
			double v2x = x1 - cx;
			double v2y = y1 - cy;
			double b = -2.0 * (v1x * v2x + v1y * v2y);
			double c = 2.0 * (v1x * v1x + v1y * v1y);
			double d2 = Math.Sqrt(b * b - 2.0 * c * (v2x * v2x + v2y * v2y - r * r));
			if (double.IsNaN(d2))
			{
				return null;
			}
			double u1 = (b - d2) / c;
			if (u1 <= 1.0 && u1 >= 0.0)
			{
				return new double[2]
				{
					x1 + v1x * u1,
					y1 + v1y * u1
				};
			}
			return null;
		}
	}
}
