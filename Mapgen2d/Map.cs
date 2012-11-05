using System;
using System.Collections.Generic;

namespace Mapgen2d {
    using PT = MapGenerator.Point;

    public class Map {
        internal Tile[,] map;

        internal List<PT> seeds;

        public Tile this[int x, int y] {
            get { return map[x, y]; }
            set { map[x, y] = value; }
        }

        public Tile this[PT pt] {
            get { return map.t( pt ); }
            set { map[pt.x,pt.y] = value; }
        }

        public int width {
            get { return map.GetLength( 0 ); }
        }

        public int height {
            get { return map.GetLength( 1 ); }
        }

        public Map( Tile[,] map ) {
            this.map = map;
        }

        public Map( int x, int y ) {
            map = new Tile[x, y];
        }

        public int[] GetRoomIDs() {
            List<int> l = new List<int>();

            for( int a = 0 ; a < seeds.Count ; a++ ) {
                PT pt = seeds[a];
                Tile tile = map.t(pt);

                if( !l.Contains( tile.group_id ) ) {
                    l.Add( tile.group_id );
                }
            }

            return l.ToArray();
        }

        public PT GetRandomPointInRoom( int group_id ) {
            List<PT> tiles = GetPointsInRoom( group_id );

            return tiles[new Random().Next( 0, tiles.Count )];
        }

        public List<PT> GetPointsInRoom( int group_id ) {
            // find seed with matching group_id
            PT seed = new PT() { x = -1, y = -1 };
            foreach( PT v in seeds ) {
                if( map.t( v ).group_id == group_id ) {
                    seed = v;
                    break;
                }
            }

            // make sure we actually found something
            if( seed.x < 0 || seed.y < 0 ) {
                Console.WriteLine( "group id " + group_id + " not found!" );
                return null;
            }

            List<PT> visited = new List<PT>();
            Queue<PT> unvisited = new Queue<PT>();

            List<PT> pointsInGroup = new List<PT>();

            unvisited.Enqueue( seed );

            while( unvisited.Count > 0 ) {
                PT pt = unvisited.Dequeue();
                visited.Add( pt );

                if( map.t( pt ).group_id == group_id ) {
                    pointsInGroup.Add( pt );
                }

                PT[] adj = MapGenerator.GetAdjacentTiles( map, pt );
                foreach( PT n in adj ) {
                    if( map.t( n ).type == Tile.Type.IMPASSABLE ) {
                        continue;
                    }

                    if( !visited.Contains( n ) ) {
                        visited.Add( n );
                        unvisited.Enqueue( n );
                    }
                }
            }

            return pointsInGroup;
        }
    }
}
