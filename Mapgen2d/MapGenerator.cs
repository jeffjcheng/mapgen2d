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
		internal struct Point {
			internal int _x;
			internal int _y;
			
			override public string ToString() {
				return _x+","+_y;
			}
		}
		
		private Random _r;
		
		public float roomBias = 0.1f;
		
		public float inflationBias = 0.7f;
		
		public float inflationDecay = 0.2f;
		
		public MapGenerator() {
			_r = new Random();
		}
		
		public void SetRandomSeed( int seed ) {
			_r = new Random( seed );
		}
		
		/// <summary>
		/// Returns an array of tiles representing a map
		/// </summary>
		public Tile[,] Generate( int x, int y ) {
			Tile[,] map = new Tile[x,y];
			
			SeedRooms( ref map );
			
			for( int a = 0 ; a < x ; a++ ) {
				for( int b = 0 ; b < y ; b++ ) {
					LinkSeeds( ref map, new Point() { _x = a, _y = b } );
				}
			}
			
			BuildRooms( ref map );
			
			return map;
		}
		
		/// <summary>
		/// Randomly place single-tile "rooms" in the map
		/// </summary>
		private void SeedRooms( ref Tile[,] map ) {
			int x = map.GetLength(0);
			int y = map.GetLength(1);
			
			// seed
			for( int a = 0 ; a < x ; a++ ) {
				for( int b = 0 ; b < y ; b++ ) {
					if( _r.NextDouble() < roomBias ) {
						map[a,b] = new Tile( Tile.Type.ROOM );
						map[a,b].group_id = Tile.next_group_id;
						
					} else {
						map[a,b] = new Tile( Tile.Type.IMPASSABLE );
					}
				}
			}
		}
		
		/// <summary>
		/// Link adjacent seed rooms
		/// </summary>
		private void LinkSeeds( ref Tile[,] map, Point p ) {
			if( p._x < 0 || p._y < 0
			 || p._x >= map.GetLength(0) || p._y >= map.GetLength(1) )
				return;
			
			if( map[p._x, p._y].type != Tile.Type.ROOM ) return;
			
			int group_id = map[p._x, p._y].group_id;
			
			Point[] p2 = GetAdjacentTiles( map, p );
			
			foreach( var pp in p2 ) {
				if( pp._x < 0 || pp._x >= map.GetLength( 0 )
				 || pp._y < 0 || pp._y >= map.GetLength( 1 ) )
					continue;
				
				if( map[pp._x, pp._y].type == Tile.Type.ROOM
				 && map[pp._x, pp._y].group_id != group_id ) {
					map[pp._x, pp._y].group_id = group_id;
					LinkSeeds( ref map, pp );
				}
			}
		}
		
		/// <summary>
		/// Grows seeded room tiles
		/// </summary>
		private void BuildRooms( ref Tile[,] map ) {
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
			
//			Console.WriteLine( "\nseeding generation has "+generation_members+" members\n" );
			
			while( seeds.Count > 0 ) {
				float chance = inflationBias - inflationDecay * current_generation;
				
				Point p = seeds.Dequeue();
				
//				Console.WriteLine( "\nseeding from "+p.ToString()+", groupid="+map[p._x, p._y].group_id );
				
				// attempt growth in all directions
				Point[] targets = GetAdjacentTiles( map, p );
				
				foreach( var target in targets ) {
					if( TrySeed( ref map, target, chance ) ) {
//						Console.WriteLine( "> growing to "+target.ToString() );
						map[target._x, target._y].group_id = map[p._x, p._y].group_id;
						seeds.Enqueue( target );
					}
				}
				
				// check for generation advancement
				if( --generation_members <= 0 ) {
					current_generation++;
					generation_members = seeds.Count;
//					Console.WriteLine( "\npreparing to iterate on generation "+current_generation+" with "+generation_members+" members" );
				}
			}
		}
		
		private void LinkRooms( ref Tile[,] map ) {
			
		}
		
		/// <summary>
		/// Tries to grow into the target position. Returns TRUE if seeding was successful
		/// </summary>
		private bool TrySeed( ref Tile[,] map, Point target, float chance ) {
			if( target._x < 0 || target._y < 0
			 || target._x >= map.GetLength(0) || target._y >= map.GetLength(1) )
				return false;
			
			if( map[target._x, target._y].type != Tile.Type.IMPASSABLE )
				return false;
			
			if( _r.NextDouble() < chance ) {
				map[target._x, target._y].type = Tile.Type.ROOM;
				return true;
			}
			
			return false;
		}
		
		private Point[] GetAdjacentTiles( Tile[,] map, Point target ) {
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
					Console.Write( (map[a,b].group_id == -1? "---" : map[a,b].group_id.ToString( "000" ))+" " );
				}
				
				Console.WriteLine();
			}
		}
	}
}
