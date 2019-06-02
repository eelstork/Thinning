using UnityEngine;
using System.Collections.Generic;

/*
A 2D thinning implementation which for all I know may or may not be working
properly. Putting in a separate repo mainly for archiving.
*/
public class Thinning {

	const int UNDEFINED = 0;

	// Note, 512 is 2^9
	public static bool[] EvalMap(){
		SelfTest();
		var map = new bool[512];
		for (int i=0;i<511;i++){
			var X = EvalInputWithCode(i);
			map[i]=CanThin(X, i);
		}
		map [511] = false;
		map [186] = false;
		map [Code (
			0, 0, 0,
			1, 1, 1,
			1, 1, 1)] = true;
		map [Code (
			1, 1, 1,
			1, 1, 1,
			0, 0, 0)] = true;
		map [Code (
			1, 1, 0,
			1, 1, 0,
			1, 1, 0)] = true;
		map [Code (
			0, 1, 1,
			0, 1, 1,
			0, 1, 1)] = true;

		return map;
	}

	static void SelfTest(){
		var h = EvalHPattern ();
		bool r = CanThin (h, 0);
		Debug.Log ("Can thin H pattern? " + r);
	}

	public static bool[,] EvalHPattern(){
		return new bool[3, 3] {
			{ true, true, true },
			{ false, true, false },
			{ true, true, true }
		};
	}

	public static int Code(params int[] table){
		int code = 0;
		int sh = 0;
		foreach (var k in table) {
			code += k << sh;
			sh++;
		}
		//ebug.Log ("Code returning: " + code);
		return code;
	}

	public static bool[,] EvalInputWithCode(int code){
		return new bool[3, 3] {
			{ Bit (0, code), Bit (1, code), Bit (2, code) },
			{ Bit (3, code), Bit (4, code), Bit (5, code) },
			{ Bit (6, code), Bit (7, code), Bit (8, code) }
		};
	}

	/*
	 Several cases considered here.
	 First, note that we don't care about any pattern where
	 The central square is already clear.
	 Rules:
	 1) If clearing the central square increases the number of set regions,
	 we cannot thin.
	 2) A "tail" is seen when there are two set flags, including the center.
	 Thinning tails would cause paths to shorten so this is disallowed.
	 3) When the cell is surrounded by set cells, it cannot be unset. In this
	 case un-setting the cell would not change the number of regions but would
	 break topology. There are several such types:

     a) @@@  b) @@o  c) o@o  d) o@o
        @@@     @@@     @@@     @@@
        @@@     @@@     o@o     @@o

     Type a) is trivial, since it is the case where all cells are set.
     Type b) includes any case where 8 cells are set.
     Type c) is 0 + 2 + 0
                8 + 16 + 32
                0 + 128 + 0 = 186 but there are extensions of c) such
     as d) which at the time of writing I do not clearly capture with a
     single rule. d) has 6 set cells so a trial rule for this might be,
     if there are 6 cells or more don't clear (therefore including a,b,c,d)
     but there are several patterns we do want to clear, for example:

     o@@
     o@@
     o@@

	 */
	static bool CanThin(bool[,] p, int code){

		Pattern pattern = new Pattern (p);
		int C = pattern.CountSetCells();
		if (C == 2) {
			//Debug.Log ("Tail found:\n" + pattern);
			return false;
		}
		if (C >= 6) {
			//ebug.Log ("6:\n" + pattern);
			return false;
		}
		//if (C > 6) {
		//	//Debug.Log ("7 or more:\n" + pattern);
		//	return false;
		//}
		//ebug.Log ("PATTERN: "+code+"\n"+pattern);
		int n0 = CountRegions(pattern);
		pattern.cells [4].value = false;
		pattern.ClearLabels ();

		int n1 = CountRegions(pattern);
		//ebug.Log("region count "+n0+" => "+n1);
		bool canThin = (n0==n1);
		//ebug.Log ("=> " + canThin);
		return canThin;
	}

	static int CountRegions(Pattern pattern){
		int n=0;
		foreach(var cell in pattern.cells){
			n = cell.AssignLabel (n);
		}
		//ebug.Log (n + " labels: ");
		//ebug.Log (pattern.LabelsToString ());
		return n;
	}

	static bool Color(int x, int y, bool[,] input){
		if(x<0 || x>2 || y<0 || y>2)return false;
		return input[x,y];
	}

	static bool[,] InputFor(int i){
		var x = new bool[3,3];
		x[0,0]=Bit(i,0); x[0,1]=Bit(i,1); x[0,2]=Bit(i,2);
		x[1,0]=Bit(i,3); x[1,1]=Bit(i,4); x[1,2]=Bit(i,5);
		x[2,0]=Bit(i,6); x[2,1]=Bit(i,7); x[2,2]=Bit(i,8);
		return x;
	}

	static bool Bit(int offset, int n){
		int k = (n >> offset) & 1;
		return k == 1 ? true : false;
	}

	class Cell{
		public Cell[] neighbours;
		public bool value;
		public int label = UNDEFINED;
		public Cell(bool x=false){ value=x; }

		public int AssignLabel(int K){
			if (!this.value) {
				//ebug.Log ("-");
				return K;
			}
			if (this.label == UNDEFINED) {
				this.label = ++K;
				//ebug.Log ("+" + this.label);
			} else {
				//ebug.Log ("x");
			}
			foreach(var neighbour in neighbours){
				if(neighbour.value == value){
					neighbour.label = label;
				}
			}
			return K;
		}

		public void AssignNeighbours(Pattern parent, params int[] indices){
			var list = new List<Cell> ();
			foreach (var k in indices) list.Add (parent.cells [k]);
			neighbours = list.ToArray ();
		}

	}

	class Pattern{

		/********
         * 0 1 2
         * 3 4 5
         * 6 7 8
	 	 ********/
		public Cell[] cells = new Cell[9];

		public Pattern(bool[,] value){
			cells[0] = new Cell( value[0,0] );
			cells[1] = new Cell( value[0,1] );
			cells[2] = new Cell( value[0,2] );
			cells[3] = new Cell( value[1,0] );
			cells[4] = new Cell( value[1,1] );
			cells[5] = new Cell( value[1,2] );
			cells[6] = new Cell( value[2,0] );
			cells[7] = new Cell( value[2,1] );
			cells[8] = new Cell( value[2,2] );
			//
			cells[0].AssignNeighbours(this, 1, 4, 3);
			cells[1].AssignNeighbours(this, 2, 5, 4, 3, 0);
			cells[2].AssignNeighbours(this, 5, 4, 1);
			cells[3].AssignNeighbours(this, 0, 1, 4, 7, 6);
			cells[4].AssignNeighbours(this, 0, 1, 2, 5, 8, 7, 6, 3);
			cells[5].AssignNeighbours(this, 8, 7, 4, 1, 2);
			cells[6].AssignNeighbours(this, 3, 4, 7);
			cells[7].AssignNeighbours(this, 6, 3, 4, 5, 8);
			cells[8].AssignNeighbours(this, 7, 4, 5);
		}

		public void ClearLabels(){
			foreach (var c in cells) c.label = UNDEFINED;
		}

		public int CountSetCells(){
			int n=0;
			foreach(var c in cells){
				if(c.value)n++;
			}
			return n;
		}

		override public string ToString(){
			return s (0) + s (1) + s (2) + "\n"
				 + s (3) + s (4) + s (5) + "\n"
			     + s (6) + s (7) + s (8);
		}

		public string LabelsToString(){
			return    l (0) + l (1) + l (2) + "\n"
    				+ l (3) + l (4) + l (5) + "\n"
			    	+ l (6) + l (7) + l (8);
		}
		public string l(int i){ return cells [i].label.ToString (); }
		public string s(int i){ return cells[i].value?"*":"-"; }

	}
}
