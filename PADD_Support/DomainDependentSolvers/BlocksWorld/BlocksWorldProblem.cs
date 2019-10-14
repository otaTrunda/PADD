using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers.BlocksWorld
{
	public class BlocksWorldProblem
	{
		public Dictionary<int, Block> blocksByIDs;
		Dictionary<string, Block> blocksBySASNames;
		HashSet<Block> blocksOnTop;
		HashSet<Block> notCorrectBlocks;
		BlocksWorldVisualizer vis;
		public Block blockInHoist;

		public List<BlocksAction> actions;

		protected void moveBlockToBlock(Block moveThis, Block putOnThis)
		{
			if (!moveThis.isOnTop() || !putOnThis.isOnTop())
				throw new ArgumentException();
			if (moveThis.targetBlockBelow != putOnThis)
				throw new Exception();
			if (!putOnThis.isCorrect())
				throw new Exception();
			if (moveThis.isCorrect())
				throw new Exception();
			Block blockBelow = moveThis.currentBlockBelow;
			if (moveThis.currentBlockBelow != null)
			{
				moveThis.currentBlockBelow.currentBlockAbove = null;
				blocksOnTop.Add(moveThis.currentBlockBelow);
			}
			moveThis.currentBlockBelow = putOnThis;
			putOnThis.currentBlockAbove = moveThis;
			blocksOnTop.Remove(putOnThis);
			notCorrectBlocks.Remove(moveThis);

			actions.Add(new BlocksAction(BlocksActionType.moveBlockToBlock, moveThis.ID, putOnThis.ID, blockBelow));
		}

		protected void moveBlockToTable(Block moveThis)
		{
			if (!moveThis.isOnTop())
				throw new ArgumentException();
			if ((moveThis.isCorrect() && this.blockInHoist != moveThis) || (moveThis.isOnTable() && blockInHoist != moveThis))
				throw new Exception();
			Block blockBelow = moveThis.currentBlockBelow;
			if (moveThis.currentBlockBelow != null)
			{
				moveThis.currentBlockBelow.currentBlockAbove = null;
				blocksOnTop.Add(moveThis.currentBlockBelow);
			}
			moveThis.currentBlockBelow = null;
			if (moveThis.isCorrect())
				notCorrectBlocks.Remove(moveThis);

			actions.Add(new BlocksAction(BlocksActionType.moveBlockToTable, moveThis.ID, -1, blockBelow));
		}

		public void init()
		{
			this.blocksOnTop = new HashSet<Block>(blocksByIDs.Values.Where(b => b.isOnTop()));
			this.notCorrectBlocks = new HashSet<Block>(blocksByIDs.Values.Where(b => !b.isCorrect()));
			this.actions = new List<BlocksAction>();
			foreach (var item in blocksByIDs.Values)
			{
				if (blocksByIDs.Values.Any(q => q.targetBlockBelow == item))
					item.targetBlockAbove = blocksByIDs.Values.Where(q => q.targetBlockBelow == item).Single();
			}
		}

		public int simulate(bool drawStates = false)
		{
			init();
			int actionsCount = 0;
			if (blockInHoist != null)
			{
				if (blockInHoist.targetBlockBelow == null)
					moveBlockToTable(blockInHoist);
				else
				{
					if (blockInHoist.targetBlockBelow.isOnTop() && blockInHoist.targetBlockBelow.isCorrect())
						moveBlockToBlock(blockInHoist, blockInHoist.targetBlockBelow);
					else
						moveBlockToTable(blockInHoist);
				}
				actionsCount++;
				blockInHoist = null;
			}
			while (notCorrectBlocks.Count > 0)
			{
				if (drawStates)
				{
					if (vis == null)
						vis = new BlocksWorldVisualizer();
					vis.draw(this);
					vis.ShowDialog();
				}

				var canBePlacedCorrectly = blocksOnTop.Where(b => !b.isCorrect() && (b.targetBlockBelow == null || (b.targetBlockBelow.isOnTop() && b.targetBlockBelow.isCorrect()))).Take(1);
				if (canBePlacedCorrectly.Any())
				{
					var block = canBePlacedCorrectly.Single();
					if (block.targetBlockBelow == null)
						moveBlockToTable(block);
					else moveBlockToBlock(block, block.targetBlockBelow);
					actionsCount += 2;
					continue;
				}
				var notCorrectBlocksOnTopNotOnTable = blocksOnTop.Where(b => !b.isOnTable() && !b.isCorrect()).Take(1);
				var blockToMove = notCorrectBlocksOnTopNotOnTable.Single();
				moveBlockToTable(blockToMove);
				actionsCount += 2;
			}
			if (drawStates)
			{
				if (vis == null)
					vis = new BlocksWorldVisualizer();
				vis.draw(this);
				vis.ShowDialog();
			}
			//	new BlocksWorldVisualizer(this).ShowDialog();
			return actionsCount;
		}

		public BlocksWorldProblem(SASProblem blockWorldInSAS)
		{
			Block.resetIDHolder();

			SASState initialState = (SASState)blockWorldInSAS.GetInitialState();
			blocksByIDs = new Dictionary<int, Block>();
			blocksBySASNames = new Dictionary<string, Block>();
			var stringSeparators = new string[] { " ", ",", "(", ")" };

			var SASVars = Enumerable.Range(0, blockWorldInSAS.GetVariablesCount());
			var blocksClearenceVars = SASVars.Where(i => blockWorldInSAS.variablesData.GetVariable(i).valuesSymbolicMeaning.Any(m => m.Contains("clear"))).ToList();

			foreach (var item in blocksClearenceVars)
			{
				int val = initialState.GetValue(item);
				string blockName = blockWorldInSAS.variablesData.GetVariable(item).GetValueSymbolicMeaning(val).Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries).Last();
				Block b = new Block(null);
				blocksByIDs.Add(b.ID, b);
				blocksBySASNames.Add(blockName, b);
				b.originalName = blockName;
			}
			var blocksPositionsVars = SASVars.Where(i => blockWorldInSAS.variablesData.GetVariable(i).valuesSymbolicMeaning.Any(m => m.Contains("on("))).ToList();

			foreach (var item in blocksPositionsVars)
			{
				int val = initialState.GetValue(item);
				string description = blockWorldInSAS.variablesData.GetVariable(item).GetValueSymbolicMeaning(val);
				if (description.Contains("holding"))
				{
					var name = description.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries).Last();
					if (this.blockInHoist != null)
						throw new ArgumentException();
					blockInHoist = blocksBySASNames[name];
					continue;
				}
				if (description.Contains("ontable"))
				{
					var name = description.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries).Last();
					blocksBySASNames[name].currentBlockBelow = null;
					continue;
				}
				if (description.Contains("on("))
				{ 
					var splitted = description.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
					string arg1 = splitted[splitted.Length - 2],
						arg2 = splitted[splitted.Length - 1];
					Block b1 = blocksBySASNames[arg1],
						b2 = blocksBySASNames[arg2];
					if (b1.currentBlockBelow != null)
						throw new ArgumentException();
					b1.currentBlockBelow = b2;
					if (b2.currentBlockAbove != null)
						throw new ArgumentException();
					b2.currentBlockAbove = b1;
				}
			}

			var goal = blockWorldInSAS.GetGoalConditions();
			foreach (var item in goal)
			{
				string description = blockWorldInSAS.variablesData.GetVariable(item.variable).GetValueSymbolicMeaning(item.value);
				if (description.Contains("holding"))
				{
					throw new ArgumentException();
				}
				if (description.Contains("ontable"))
				{
					var name = description.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries).Last();
					blocksBySASNames[name].targetBlockBelow = null;
					continue;
				}
				if (description.Contains("on("))
				{
					var splitted = description.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
					string arg1 = splitted[splitted.Length - 2],
						arg2 = splitted[splitted.Length - 1];
					Block b1 = blocksBySASNames[arg1],
						b2 = blocksBySASNames[arg2];
					if (b1.targetBlockBelow != null)
						throw new ArgumentException();
					b1.targetBlockBelow = b2;
				}
			}
		}
	}

	public class Block
	{
		protected static int IDHolder = 0;

		public static void resetIDHolder()
		{
			IDHolder = 0;
		}

		public int ID { get; protected set; }
		public Block currentBlockBelow;
		public Block currentBlockAbove;
		public Block targetBlockBelow, targetBlockAbove;
		public string originalName;

		public bool isTargetBelowSpecified => targetBlockBelow != null;
		public bool isTargetAboveSpecified => targetBlockAbove != null;

		public Block(Block targetBlockBelow)
		{
			this.ID = IDHolder++;
			this.currentBlockBelow = null;
			this.currentBlockAbove = null;
			this.targetBlockBelow = targetBlockBelow;
		}

		public Block(Block currentBlockBelow, Block targetBlockBelow, Block currentBlockAbove = null)
		{
			this.ID = IDHolder++;
			this.currentBlockBelow = currentBlockBelow;
			this.currentBlockAbove = currentBlockAbove;
			this.targetBlockBelow = targetBlockBelow;
		}

		public override string ToString()
		{
			return ID.ToString();
		}

		public bool isCorrect()
		{
			if (!isTargetBelowSpecified)
			{
				if (currentBlockBelow == null)
					return true;
				if (currentBlockBelow.isTargetAboveSpecified && currentBlockBelow.targetBlockAbove != this)
					return false;
				return currentBlockBelow.isCorrect();
			}

			if (targetBlockBelow == null && currentBlockBelow == null)
				return true;
			if (currentBlockBelow == null)
				return false;
			return currentBlockBelow.ID == targetBlockBelow.ID && currentBlockBelow.isCorrect();
		}

		public bool isOnTop()
		{
			return currentBlockAbove == null;
		}

		public bool isOnTable()
		{
			return this.currentBlockBelow == null;
		}

		public override int GetHashCode()
		{
			return this.ID;
		}

		public override bool Equals(object obj)
		{
			if (obj is Block)
				return ((Block)obj).ID == this.ID;
			return false;
		}
	}

	public class BlocksAction
	{
		public BlocksActionType type;
		public int block1ID, block2ID;
		public Block blockBelowTheFirst;

		public BlocksAction(BlocksActionType type, int block1ID, int block2ID, Block blockBelowTheFirst)
		{
			this.type = type;
			this.block1ID = block1ID;
			this.block2ID = block2ID;
			this.blockBelowTheFirst = blockBelowTheFirst;
		}
	}

	public enum BlocksActionType
	{
		moveBlockToBlock,
		moveBlockToTable
	}
}
