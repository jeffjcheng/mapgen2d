using System;

namespace mapgen {
	public class Tile {
		private static int group_id_counter = 0;
		internal static int next_group_id {
			get {
				return group_id_counter++;
			}
		}
		
		public enum Type {
			HALL,
			ROOM,
			IMPASSABLE,
		}
		
		public Tile.Type type {
			internal set; get;
		}
		
		public int group_id {
			internal set; get;
		}
		
		public bool HasPassage( MapDirection dir ) {
			return _passage[(int)dir];
		}
		
		public void SetPassage( MapDirection dir, bool passable ) {
			_passage[(int)dir] = passable;
		}
		
		internal bool[] _passage;
		
		internal Tile( Type type = Type.IMPASSABLE) {
			this.type = type;
			group_id = -1;
			
			switch( this.type ) {
			case Type.HALL:
				_passage = new bool[] { false, false, false, false };
				break;
				
			case Type.ROOM:
				_passage = new bool[] { true, true, true, true };
				break;
				
			case Type.IMPASSABLE:
				_passage = new bool[] { false, false, false, false };
				break;
			}
		}
	}
}

