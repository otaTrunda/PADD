﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.DomainDependentSolvers.BlocksWorld
{
	public class BlocksWorldProblem
	{
		public List<Block> blocksByIDs;
		Dictionary<string, Block> blocksBySASNames;
		HashSet<Block> blocksOnTop;
		HashSet<Block> notCorrectBlocks;

		Block blockInHoist;

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
			if (moveThis.currentBlockBelow != null)
			{
				moveThis.currentBlockBelow.currentBlockAbove = null;
				blocksOnTop.Add(moveThis.currentBlockBelow);
			}
			moveThis.currentBlockBelow = putOnThis;
			putOnThis.currentBlockAbove = moveThis;
			blocksOnTop.Remove(putOnThis);
			notCorrectBlocks.Remove(moveThis);
		}

		protected void moveBlockToTable(Block moveThis)
		{
			if (!moveThis.isOnTop())
				throw new ArgumentException();
			if (moveThis.isCorrect() || moveThis.isOnTable())
				throw new Exception();
			moveThis.currentBlockBelow.currentBlockAbove = null;
			blocksOnTop.Add(moveThis.currentBlockBelow);
			moveThis.currentBlockBelow = null;
			if (moveThis.isCorrect())
				notCorrectBlocks.Remove(moveThis);
		}

		public void init()
		{
			this.blocksOnTop = new HashSet<Block>(blocksByIDs.Where(b => b.isOnTop()));
			this.notCorrectBlocks = new HashSet<Block>(blocksByIDs.Where(b => !b.isCorrect()));
		}

		public int simulate(bool drawStates = false)
		{
			init();
			int actionsCount = 0;
			if (blockInHoist != null)
			{
				if (blockInHoist.targetBlockBelow.isOnTop() && blockInHoist.targetBlockBelow.isCorrect())
					moveBlockToBlock(blockInHoist, blockInHoist.targetBlockBelow);
				actionsCount++;
				blockInHoist = null;
			}
			while (notCorrectBlocks.Count > 0)
			{
				if (drawStates)
					new BlocksWorldVisualizer(this).ShowDialog();

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
			return actionsCount;
		}

		public BlocksWorldProblem(SASProblem blockWorldInSAS)
		{
			SASState initialState = (SASState)blockWorldInSAS.GetInitialState();
			blocksByIDs = new List<Block>();
			blocksBySASNames = new Dictionary<string, Block>();
			var stringSeparators = new string[] { " ", ",", "(", ")" };

			var SASVars = Enumerable.Range(0, blockWorldInSAS.GetVariablesCount());
			var blocksClearenceVars = SASVars.Where(i => blockWorldInSAS.variablesData.GetVariable(i).valuesSymbolicMeaning.Any(m => m.Contains("clear"))).ToList();

			foreach (var item in blocksClearenceVars)
			{
				int val = initialState.GetValue(item);
				string blockName = blockWorldInSAS.variablesData.GetVariable(item).GetValueSymbolicMeaning(val).Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries).Last();
				Block b = new Block(null);
				blocksByIDs.Add(b);
				blocksBySASNames.Add(blockName, b);
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
						arg2 = splitted[splitted.Length - 2];
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
					blocksBySASNames[name].isTargetSpecified = true;
					continue;
				}
				if (description.Contains("on("))
				{
					var splitted = description.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
					string arg1 = splitted[splitted.Length - 2],
						arg2 = splitted[splitted.Length - 2];
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

		public int ID { get; protected set; }
		public Block currentBlockBelow;
		public Block currentBlockAbove;
		public Block targetBlockBelow;

		public bool isTargetSpecified = false;

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

		public bool isCorrect()
		{
			if (!isTargetSpecified)
			{
				if (targetBlockBelow == null)
					return true;
				return targetBlockBelow.isCorrect();
			}
			if (targetBlockBelow == null && currentBlockBelow == null)
				return true;
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
}