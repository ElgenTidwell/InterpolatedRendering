using System;
using System.Numerics;
using Raylib_cs;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SBTech.InterpRasterization
{
	public class Camera
	{
		public Vector3 worldPos, forward, right, up;
		public Vector3 rotation;
		public float fov = 90;
	}

	public class Node
	{
		public Vector3 position;
		public Vector3 handlel,handler;
	}

	public class Handle
	{
		public float distance,rotlation;
	}

	internal class Engine
	{
		static int w = 1600, h = 900, _universalLOD = 1;

		static bool[,] edges;

		static Camera cam = new Camera();

		static List<Node> batch = new List<Node>();

		static void Main(string[] args)
		{
			cam.worldPos = Vector3.Zero - Vector3.UnitZ*5 - Vector3.UnitY * 5;
			cam.rotation = Vector3.Zero;

			Node n = new Node();
			n.position = new Vector3(1,1,-0.1f);
			n.handlel = n.position + Vector3.UnitY*2 + Vector3.UnitZ * 2;
			n.handler = n.position + Vector3.UnitY * 2;

			Node n1 = new Node();
			n1.position = new Vector3(-1, -1, 0);

			Node n2 = new Node();
			n2.position = new Vector3(-1,2, 0);

			batch.Add(n);

			batch.Add(n2);

			batch.Add(n1);

			batch.Add(n);

			float yrot = 0, xrot = 0;
			Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT);
			Raylib.InitWindow(w,h, "Interpolated Rasterization");

			Node[] model = LoadModel("./Models/base_wip.obj").ToArray();

			while (!Raylib.WindowShouldClose() && !Raylib.IsKeyDown(KeyboardKey.KEY_ESCAPE))
			{
				Raylib.BeginDrawing();
				Raylib.ClearBackground(Color.BLACK);

				DrawNodeBatch(batch);

				if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT))
					yrot -= Raylib.GetFrameTime();
				if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT))
					yrot += Raylib.GetFrameTime();
				if (Raylib.IsKeyDown(KeyboardKey.KEY_UP))
					xrot += Raylib.GetFrameTime();
				if (Raylib.IsKeyDown(KeyboardKey.KEY_DOWN))
					xrot -= Raylib.GetFrameTime();

				if (Raylib.IsKeyDown(KeyboardKey.KEY_EQUAL))
					cam.fov += Raylib.GetFrameTime() * 10f;
				if (Raylib.IsKeyDown(KeyboardKey.KEY_MINUS))
					cam.fov -= Raylib.GetFrameTime() * 10f;

				if (_universalLOD < 1)
				{
					_universalLOD = 1;
				}

				Raylib.DisableCursor();

				xrot = (-Raylib.GetMouseY() / 1000f) *1.5f;
				yrot = Raylib.GetMouseX() / 1000f;

				cam.rotation = new Vector3(xrot, MathF.Max(MathF.Min(yrot,90),-90), 0);

				cam.forward = new Vector3(MathF.Sin(cam.rotation.Y), 0, MathF.Cos(cam.rotation.Y));
				cam.right = new Vector3(cam.forward.Z, 0, -cam.forward.X);

				if (Raylib.IsKeyDown(KeyboardKey.KEY_W))
					cam.worldPos += cam.forward * Raylib.GetFrameTime()*2;
				if (Raylib.IsKeyDown(KeyboardKey.KEY_S))
					cam.worldPos -= cam.forward * Raylib.GetFrameTime() * 2;
				if (Raylib.IsKeyDown(KeyboardKey.KEY_A))
					cam.worldPos -= cam.right * Raylib.GetFrameTime() * 2;
				if (Raylib.IsKeyDown(KeyboardKey.KEY_D))
					cam.worldPos += cam.right * Raylib.GetFrameTime() * 2;

				if (Raylib.IsKeyDown(KeyboardKey.KEY_SPACE))
					cam.worldPos -= Vector3.UnitY * Raylib.GetFrameTime();
				if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL))
					cam.worldPos += Vector3.UnitY * Raylib.GetFrameTime();

				Raylib.EndDrawing();
			}
			Raylib.CloseWindow();
		}

		public static void DrawNodeBatch(List<Node> nodes)
		{
			Vector2 oldpos = ProjectPointToScreen2D(nodes[0].position,cam);

			List<Vector2> verts = new List<Vector2>();

			for(int i = 0; i < nodes.Count-1; i ++)
			{
				Node n = nodes[i];
				Node n1 = nodes[i+1];

				Vector3 p1 = ProjectPointToScreen(n.position, cam), p2 = ProjectPointToScreen(n1.position, cam);

				//Raylib.DrawCircleLines((int)p1.X,  (int)p1.Y,  6 * p1.Z, Color.WHITE);
				//Raylib.DrawCircleLines((int)p2.X, (int)p2.Y, 6 * p2.Z, Color.WHITE);

				
				Vector3 nn = n.position+n.handlel;
				Vector3 nn1 = n1.position+n1.handlel;
				//
				//Raylib.DrawLineBezierQuad(ProjectPointToScreen2D(n.position,cam), ProjectPointToScreen2D(n1.position, cam), ProjectPointToScreen2D(Vector3.Lerp(nn, nn1, 0.5f),cam), 5,Color.WHITE);
				int vertindex = 0;
				for(float j = 0; j <= 1; j += 0.01f*(_universalLOD + (Vector3.Distance(n1.position,cam.worldPos))))
				{
					vertindex++;

					Vector2 pos = ProjectPointToScreen2D(CalculateCubicBezierPoint(j, n.position, nn, nn1, n1.position), cam);

					verts.Insert(0,pos);

					if (pos == new Vector2(-w * 2, -h * 2) || oldpos == new Vector2(-w * 2, -h * 2))
					{
						oldpos = pos;
						continue;
					}

					Raylib.DrawLineEx(pos, oldpos, 2, Color.WHITE);

					oldpos = pos;
				}
				
				verts.Clear();
			}
		}

		static List<Node> LoadModel(string filename)
		{
			List<Node> nodes = new List<Node>();

			if (!File.Exists(filename))
			{
				Console.WriteLine($"ERR: File, {filename}, can not be found.");

				return null;
			}

			string[] data = File.ReadAllLines(filename);

			foreach(string line in data)
			{
				if (!line.StartsWith('v')) continue;
				line.Remove(0, 2);

				string[] floats = line.Split(' ');

				float v1 = (float)double.Parse(floats[1]) / 4;
				float v2 = -(float)double.Parse(floats[2]) / 4;
				float v3 = 3>=floats.Length? 0f :(float)double.Parse(floats[3]) / 4;

				Node node = new Node();
				node.position = new Vector3(v1,v2,v3);
				nodes.Add(node);
			}
			return nodes;
		}

		static Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
		{
			float u = 1 - t;
			float tt = t * t;
			float uu = u * u;
			float uuu = uu * u;
			float ttt = tt * t;

			Vector3 p = uuu * p0;
			p += 3 * uu * t * p1;
			p += 3 * u * tt * p2;
			p += ttt * p3;

			return p;
		}

		static private Vector3 ProjectPointToScreen(Vector3 point,Camera cam)
		{
			Vector3 b = Vector3.Zero;
			Vector3 d = Vector3.Zero;

			float X = point.X - cam.worldPos.X, Y = point.Y - cam.worldPos.Y, Z = point.Z - cam.worldPos.Z;

			float S(float i) => MathF.Sin(i);
			float C(float i) => MathF.Cos(i);

			d.X = C(cam.rotation.Y) * (S(cam.rotation.Z) * Y + C(cam.rotation.Z) * X) - S(cam.rotation.Y) * Z;
			d.Y = S(cam.rotation.X) * (C(cam.rotation.Y) * Z + S(cam.rotation.Y) * (S(cam.rotation.Z) * Y + C(cam.rotation.Z) * X)) + C(cam.rotation.X) * (C(cam.rotation.Z)*Y-S(cam.rotation.Z)*X);
			d.Z = C(cam.rotation.X) * (C(cam.rotation.Y) * Z + S(cam.rotation.Y) * (S(cam.rotation.Z) * Y + C(cam.rotation.Z) * X)) - S(cam.rotation.X) * (C(cam.rotation.Z)*Y-S(cam.rotation.Z)*X);

			if(cam.rotation == Vector3.Zero)
			{
				d = point - cam.worldPos;
			}

			if (d.Z < 0)
			{
				return new Vector3(-w*2,-h*2,-w*2);
			}

			b.X = (d.X*h/(cam.fov*(MathF.PI/180)))/(d.Z) + (w/2);
			b.Y = (d.Y*h/(cam.fov * (MathF.PI / 180))) /(d.Z) + (h/2);
			b.Z = d.Z;

			return b;
		}
		static private Vector2 ProjectPointToScreen2D(Vector3 point, Camera cam)
		{
			Vector3 fin = ProjectPointToScreen(point, cam);

			return new Vector2(fin.X,fin.Y);
		}
	}
}