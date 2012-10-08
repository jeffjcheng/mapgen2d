using System;
using System.Collections.Generic;

namespace Mapgen2d {
	public enum MapDirection {
		NORTH,
		SOUTH,
		WEST,
		EAST
	}
	
	public class MapGenerator {
		public struct Point {
			internal int _x;
			internal int _y;
			
			override public string ToString() {
				return _x+","+_y;
			}
			
			public static bool operator ==( Point p1, Point p2 ) {
				return p1._x == p2._x && p1._y == p2._y;
			}
			
			public static bool operator !=( Point p1, Point p2 ) {
				return p1._x != p2._x || p1._y != p2._y;
			}
		}
		
		private Random _r;
		
		private List<Point> seeds;
		
		public float roomBias = 0.1f;
		
		public float inflationBias = 0.7f;
		
		public float inflationDecay = 0.2f;
		
		public float excessHallwayModifier = 0.1f;
		
		public int LastSeed { private set; get; }
		
		private int seed = -1;
		
		public MapGenerator() {}
		
		public void SetRandomSeed( int seed ) {
			this.seed = seed;
			_r = new Random( seed );
		}
		
		/// <summary>
		/// Returns an array of tiles representing a map
		/// </summary>
		public Tile[,] Generate( int x, int y ) {
			if( seed == -1 ) {
				seed = DateTime.UtcNow.Ticks.GetHashCode();
				_r = new Random( seed );
			}
			
			Tile[,] map = new Tile[x,y];
			
			DateTime time_start = DateTime.UtcNow;
			
			SeedRooms( map );
			Console.WriteLine( "seeding completed" );
			
			for( int a = 0 ; a < x ; a++ ) {
				for( int b = 0 ; b < y ; b++ ) {
					LinkSeeds( map, new Point() { _x = a, _y = b } );
				}
			}
			Console.WriteLine( "seed links completed" );
			
			BuildRooms( map );
			Console.WriteLine( "room expansion completed" );
			
			LinkRooms( map );
			Console.WriteLine( "room linking completed" );
			
			DateTime time_end = DateTime.UtcNow;
			Console.WriteLine( "map generation took "+(time_end.Subtract( time_start)).TotalSeconds );
#if UNITY_ENGINE
			Debug.Log( "map generation took "+(time_end.Subtract( time_start )).TotalSeconds );
#endif
			
			LastSeed = seed;
			seed = -1;
			
			return map;
		}
		
		/// <summary>
		/// Randomly place single-tile "rooms" in the map
		/// </summary>
		private void SeedRooms( Tile[,] map ) {
			seeds = new List<Point>();
			int x = map.GetLength(0);
			int y = map.GetLength(1);
			
			// seed
			for( int a = 0 ; a < x ; a++ ) {
				for( int b = 0 ; b < y ; b++ ) {
					if( _r.NextDouble() < roomBias ) {
						map[a,b] = new Tile( Tile.Type.ROOM );
						map[a,b].group_id = Tile.next_group_id;
						seeds.Add( new MapGenerator.Point() { _x = a, _y = b } );
					} else {
						map[a,b] = new Tile( Tile.Type.IMPASSABLE );
					}
				}
			}
		}
		
		/// <summary>
		/// Link adjacent seed rooms
		/// </summary>
		private void LinkSeeds( Tile[,] map, Point p ) {
			if( p._x < 0 || p._y < 0
			 || p._x >= map.GetLength(0) || p._y >= map.GetLength(1) )
				return;
			
			if( map.t(p).type != Tile.Type.ROOM ) return;
			
			int group_id = map.t(p).group_id;
			
			Point[] p2 = GetAdjacentTiles( map, p );
			
			foreach( var pp in p2 ) {
				if( pp._x < 0 || pp._x >= map.GetLength( 0 )
				 || pp._y < 0 || pp._y >= map.GetLength( 1 ) )
					continue;
				
				if( map.t(pp).type == Tile.Type.ROOM
				 && map.t(pp).group_id != group_id ) {
					AllowPassage( map, p, pp );
					map.t(pp).group_id = group_id;
					LinkSeeds( map, pp );
				}
			}
		}
		
		private void AllowPassage( Tile[,] map, Point p1, Point p2 ) {
			int dx = p1._x - p2._x;
			int dy = p1._y - p2._y;
			
			if( Math.Abs( dx ) > 1 || Math.Abs( dy ) > 1 ) {
				Console.WriteLine( p1 + " and " + p2 + " are not adjacent!" );
				return;
			}
			
			if( dx != 0 ^ dy == 0 ) {
				Console.WriteLine( p1 + " and " + p2 + " are not adjacent!" );
				return;
			}
			
			if( dx < 0 ) {
				map.t(p1).SetPassage( MapDirection.EAST, true );
				map.t(p2).SetPassage( MapDirection.WEST, true );
			} else if ( dx > 0 ) {
				map.t(p1).SetPassage( MapDirection.WEST, true );
				map.t(p2).SetPassage( MapDirection.EAST, true );
			} else if( dy < 0 ) {
				map.t(p1).SetPassage( MapDirection.SOUTH, true );
				map.t(p2).SetPassage( MapDirection.NORTH, true );
			} else if( dy > 0 ) {
				map.t(p1).SetPassage( MapDirection.NORTH, true );
				map.t(p2).SetPassage( MapDirection.SOUTH, true );
			}
		}
		
		/// <summary>
		/// Grows seeded room tiles
		/// </summary>
		private void BuildRooms( Tile[,] map ) {
			Queue<Point> seeds = new Queue<Point>();
			
			int x = map.GetLength(0);
			int y = map.GetLength(1);
			
			// find seed locations
			for( int a = 0 ; a < x ; a++ ) {
				for( int b = 0 ; b < y ; b++ ) {
					if( map[a,b].type == Tile.Type.ROOM ) {
						seeds.Enqueue( new MapGenerator.Point() { _x = a, _y = b } );
					}
				}
			}
			
			int current_generation = 0;
			int generation_members = seeds.Count;
			
			while( seeds.Count > 0 ) {
				float chance = inflationBias - inflationDecay * current_generation;
				
				Point p = seeds.Dequeue();
				
				// attempt growth in all directions
				Point[] targets = GetAdjacentTiles( map, p );
				
				foreach( var target in targets ) {
					if( TrySeed( map, target, chance ) ) {
						AllowPassage( map, p, target );
						map.t(target).group_id = map[p._x, p._y].group_id;
						seeds.Enqueue( target );
					}
				}
				
				// check for generation advancement
				if( --generation_members <= 0 ) {
					current_generation++;
					generation_members = seeds.Count;
				}
			}
		}
		
		private void LinkRooms( Tile[,] map ) {
			for( int a = 0 ; a < seeds.Count ; a++ ) {
				for( int b = a+1 ; b < seeds.Count ; b++ ) {
					Point p1 = seeds[a];
					Point p2 = seeds[b];
					
					if( CheckConnectivity( map, p1, p2 ) && _r.NextDouble() > excessHallwayModifier ) {
						break;
					}
					
					// diffs
					int dx = p1._x - p2._x;
					int dy = p1._y - p2._y;
					
					// indices
					int ix = p1._x;
					int iy = p1._y;
					
					// direction randomiser
					float rx;
					
					while( ix != p2._x || iy != p2._y ) {
						dx = ix - p2._x;
						dy = iy - p2._y;
						
						Point t1 = new Point() { _x=ix, _y=iy };
						
						rx = Math.Abs( (float)dx / (float)(dx + dy) );
						
						if( _r.NextDouble() < rx ) {
							ix -= Math.Sign( dx );
						} else {
							iy -= Math.Sign( dy );
						}
						
						Point t2 = new Point() { _x=ix, _y=iy };
						
						if( map[ix,iy].type == Tile.Type.IMPASSABLE ) {
							map[ix,iy].type = Tile.Type.HALL;
							AllowPassage( map, t1, t2 );
						}
					}
				}
			}
		}
		
		public static bool CheckConnectivity( Tile[,] map, Point source, Point target ) {
			if( map.t(source).type == Tile.Type.IMPASSABLE )
				return false;
			
			if( map.t(target).type == Tile.Type.IMPASSABLE )
				return false;
			
			List<Point> visited = new List<Point>();
			Queue<Point> unvisited = new Queue<Point>();
			
			unvisited.Enqueue( source );
			
			while( unvisited.Count > 0 ) {
				Point p = unvisited.Dequeue();
				visited.Add( p );
				
				
				if( p == target ) {
					return true;
				}
				
				Point[] adj = GetAdjacentTiles( map, p );
				foreach( Point n in adj ) {
					if( map.t(n).type == Tile.Type.IMPASSABLE ) {
						continue;
					}
					
					if( !visited.Contains( n ) ) {
						visited.Add( n );
						unvisited.Enqueue( n );
					}
				}
			}
			
			return false;
		}
		
		/// <summary>
		/// Tries to grow into the target position. Returns TRUE if seeding was successful
		/// </summary>
		private bool TrySeed( Tile[,] map, Point target, float chance ) {
			if( target._x < 0 || target._y < 0
			 || target._x >= map.GetLength(0) || target._y >= map.GetLength(1) )
				return false;
			
			if( map.t(target).type != Tile.Type.IMPASSABLE )
				return false;
			
			if( _r.NextDouble() < chance ) {
				map.t(target).type = Tile.Type.ROOM;
				return true;
			}
			
			return false;
		}
		
		public static Point[] GetAdjacentTiles( Tile[,] map, Point target ) {
			List<Point> pts = new List<Point>();
			
			if( target._x > 0 )
				pts.Add( new Point() { _x = target._x-1, _y = target._y } );
			
			if( target._x < map.GetLength( 0 )-1 )
				pts.Add( new Point() { _x = target._x+1, _y = target._y } );
			
			if( target._y > 0 )
				pts.Add( new Point() { _x = target._x, _y = target._y-1 } );
			
			if( target._y < map.GetLength( 1 )-1 )
				pts.Add( new Point() { _x = target._x, _y = target._y+1 } );
			
			return pts.ToArray();
		}
		
		public static void PrintMap( Tile[,] map ) {
			Console.WriteLine( "Map dimensions: "+map.GetLength(0)+","+map.GetLength(1) );
			
			int x = map.GetLength( 0 );
			int y = map.GetLength( 1 );
			
			for( int a = 0 ; a < x ; a++ ) {
				for( int b = 0 ; b < y ; b++ ) {
					Tile t = map[a,b];
					
					switch( t.type ) {
					case Tile.Type.HALL:
						Console.Write( "+++" );
						break;
						
					case Tile.Type.ROOM:
						Console.Write( t.group_id.ToString( "000" ) );
						break;
						
					default: // or IMPASSABLE
						Console.Write( "---" );
						break;
					}
					
					Console.Write( " " );
				}
				
				Console.WriteLine();
			}
		}
	}

	public static class MapGenExt {
		public static Tile t( this Mapgen2d.Tile[,] map, Mapgen2d.MapGenerator.Point pt ) {
			return map[pt._x,pt._y];
		}	
	}
}
