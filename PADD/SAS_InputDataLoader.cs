using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Auxiliary static class for creation and filling of SASInputData instance from the given SAS+ input file.
    /// </summary>
    public static class SASInputDataLoader
    {
        /// <summary>
        /// Creates and fills an input data structure for SAS+ planning problem from the specified input file.
        /// </summary>
        /// <param name="inputFile">SAS+ input file.</param>
        /// <returns>New instance of SAS+ input data.</returns>
        public static SASInputData LoadFromFile(string inputFile)
        {
            SASInputData inputData = new SASInputData();
            ReadFile(inputFile, inputData);
            return inputData;
        }

        /// <summary>
        /// Current line number of the input file.
        /// </summary>
        private static int lineNumber;

        /// <summary>
        /// Static reference for SAS+ input data structure being loaded from the input file.
        /// </summary>
        private static SASInputData loadedInputData;

        /// <summary>
        /// Extracts the input data from a SAS+ input file.
        /// </summary>
        /// <param name="fileName">SAS+ input file.</param>
        /// <param name="inputData">Input data structure to be filled.</param>
        private static void ReadFile(string fileName, SASInputData inputData)
        {
            // set the static reference for the loading process
            loadedInputData = inputData;
            lineNumber = 0;

            // process the input file
            SetProblemNameAndFilePath(fileName);
            using (var reader = new System.IO.StreamReader(fileName))
            {
                ProcessVersion(reader);
                ProcessMetric(reader);
                ProcessVariables(reader);
                ProcessMutexGroups(reader);
                ProcessInitState(reader);
                ProcessGoalState(reader);
                ProcessOperators(reader);
                ProcessAxiomRules(reader);
            }

            // free the static reference
            loadedInputData = null;
        }

        /// <summary>
        /// Fetches the next line from the file reader.
        /// </summary>
        /// <param name="reader">Reader of the SAS+ input file.</param>
        /// <returns>Next line of the input file.</returns>
        private static string GetNextLine(StreamReader reader)
        {
            ++lineNumber;
            return reader.ReadLine();
        }

        /// <summary>
        /// Splits the given line string by whitespaces into a list of tokens.
        /// </summary>
        /// <param name="line">Line string.</param>
        /// <returns>List of string tokens.</returns>
        private static string[] SplitByWhiteSpaces(string line)
        {
            return line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Checks the correctness of the given line of the input file. Throws SASInputDataLoaderException, if the line is incorrect.
        /// </summary>
        /// <param name="fileLine">Line to be checked.</param>
        /// <param name="expectedContent">Expected content of the line.</param>
        /// <param name="errID">Error ID to be thrown, if the input is incorrect.</param>
        private static void CheckLine(string fileLine, string expectedContent, SASErrorID errID)
        {
            if (!String.Equals(fileLine, expectedContent))
                throw new SASInputDataLoaderException(errID, lineNumber, expectedContent, fileLine);
        }

        /// <summary>
        /// Checks the correctness of the given line of the input file. Throws SASInputDataLoaderException, if the line is incorrect.
        /// </summary>
        /// <param name="fileLine">Line to be parsed and checked.</param>
        /// <param name="expectedContent">Expected content of the line.</param>
        /// <param name="errID">Error ID to be thrown, if the input is incorrect.</param>
        private static void CheckLine(string fileLine, uint expectedContent, SASErrorID errID)
        {
            uint actualNum;
            if (!uint.TryParse(fileLine, out actualNum))
            {
                throw new SASInputDataLoaderException(SASErrorID.UnsignedIntegerExpected, lineNumber, fileLine);
            }

            if (actualNum != expectedContent)
                throw new SASInputDataLoaderException(errID, lineNumber, fileLine);
        }

        /// <summary>
        /// Type of number to be parsed and checked.
        /// </summary>
        private enum NumType
        {
            UnsignedInteger, UnsignedIntegerOrMinusOne
        }

        /// <summary>
        /// Checks whether the given line is a correct number of the specified type and returns it. Throws SASInputDataLoaderException, if the line is incorrect.
        /// </summary>
        /// <param name="fileLine">Line to be parsed and checked.</param>
        /// <param name="numType">Type of input number to be parsed.</param>
        private static int ParseAndCheckNum(string fileLine, NumType numType = NumType.UnsignedInteger)
        {
            int actualNum;
            bool valid = int.TryParse(fileLine, out actualNum);

            if (valid)
                valid = (actualNum >= (numType == NumType.UnsignedIntegerOrMinusOne ? -1 : 0));

            if (!valid)
            {
                if (numType == NumType.UnsignedIntegerOrMinusOne)
                    throw new SASInputDataLoaderException(SASErrorID.UnsignedIntegerOrMinusOneExpected, lineNumber, fileLine);
                else
                    throw new SASInputDataLoaderException(SASErrorID.UnsignedIntegerExpected, lineNumber, fileLine);
            }

            return actualNum;
        }

        /// <summary>
        /// Type of numeric list to be parsed and checked.
        /// </summary>
        private enum NumListType
        {
            VariableValuePair, OperatorEffect, AxiomRuleHead
        }

        /// <summary>
        /// Checks whether the given line is a list of correct unsigned integer numbers and returns it. Throws SASInputDataLoaderException, if the line is incorrect.
        /// </summary>
        /// <param name="fileLine">Line to be parsed and checked.</param>
        /// <param name="numListType">Type of input list to be parsed.</param>
        /// <returns>Parsed list of unsigned numeric values.</returns>
        private static List<int> ParseAndCheckNumList(string fileLine, NumListType numListType)
        {
            List<int> retList = new List<int>();
            string[] listStr = SplitByWhiteSpaces(fileLine);

            int expectedListLength = 0;
            switch (numListType)
            {
                case NumListType.VariableValuePair:
                    expectedListLength = 2;
                    break;
                case NumListType.OperatorEffect:
                    uint condEffCount;
                    if (listStr.Length < 1 || !uint.TryParse(listStr[0], out condEffCount))
                        throw new SASInputDataLoaderException(SASErrorID.InvalidNumList, lineNumber, "at least 4", fileLine);
                    expectedListLength = 1 + (int)condEffCount * 2 + 3;
                    break;
                case NumListType.AxiomRuleHead:
                    expectedListLength = 3;
                    break;
            }

            if (listStr.Length != expectedListLength)
                throw new SASInputDataLoaderException(SASErrorID.InvalidNumList, lineNumber, expectedListLength.ToString(), fileLine);

            for (int i = 0; i < listStr.Length; ++i)
            {
                string str = listStr[i];

                int actualNum;
                if (!int.TryParse(str, out actualNum))
                    throw new SASInputDataLoaderException(SASErrorID.InvalidNumList, lineNumber, expectedListLength.ToString(), fileLine);

                int lowerBound = 0;
                if ((numListType == NumListType.AxiomRuleHead || numListType == NumListType.OperatorEffect) && i == listStr.Length-2)
                    lowerBound = -1;

                if (actualNum < lowerBound)
                    throw new SASInputDataLoaderException(SASErrorID.InvalidNumList, lineNumber, expectedListLength.ToString(), fileLine);

                retList.Add(actualNum);
            }

            return retList;
        }

        /// <summary>
        /// Checks whether the given variable was correctly defined. Throws SASInputDataLoaderException, if the variable is invalid.
        /// </summary>
        /// <param name="varID">Varible ID.</param>
        private static void CheckDefinedVariable(int varID)
        {
            if (varID >= loadedInputData.Variables.Count)
                throw new SASInputDataLoaderException(SASErrorID.InvalidVaribleUsed, lineNumber, varID.ToString());
        }

        /// <summary>
        /// Checks whether the given value is within defined domain range of the given variable. Throws SASInputDataLoaderException, if the value is invalid.
        /// </summary>
        /// <param name="varID">Variable ID.</param>
        /// <param name="valueID">Value ID.</param>
        private static void CheckValueForVariable(int varID, int valueID)
        {
            if (valueID >= loadedInputData.Variables[varID].DomainRange)
                throw new SASInputDataLoaderException(SASErrorID.InvalidValueForVarible, lineNumber, valueID.ToString(), varID.ToString());
        }

        /// <summary>
        /// Sets the problem name. In SAS+ input files, we can't specify the problem name, so it is derived from the file name.
        /// </summary>
        private static void SetProblemNameAndFilePath(string fileName)
        {
            loadedInputData.ProblemName = Path.GetFileNameWithoutExtension(fileName).Trim();
            loadedInputData.InputFilePath = fileName;
        }

        /// <summary>
        /// Processes the SAS+ version block.
        /// </summary>
        /// <param name="reader">Reader of the SAS+ input file.</param>
        private static void ProcessVersion(StreamReader reader)
        {
            CheckLine(GetNextLine(reader), "begin_version", SASErrorID.InvalidSectionStart);

            string version = GetNextLine(reader);
            CheckLine(version, 3, SASErrorID.UnsupportedVersion);
            loadedInputData.InputFileVersion = int.Parse(version);

            CheckLine(GetNextLine(reader), "end_version", SASErrorID.InvalidSectionEnd);
        }

        /// <summary>
        /// Processes the SAS+ metric block.
        /// </summary>
        /// <param name="reader">Reader of the SAS+ input file.</param>
        private static void ProcessMetric(StreamReader reader)
        {
            CheckLine(GetNextLine(reader), "begin_metric", SASErrorID.InvalidSectionStart);

            string metricStr = GetNextLine(reader);
            int metric = ParseAndCheckNum(metricStr);

            if (metric == 0 || metric == 1)
                loadedInputData.IsMetricUsed = (metric == 1);
            else
                throw new SASInputDataLoaderException(SASErrorID.InvalidMetric, lineNumber, metricStr);

            CheckLine(GetNextLine(reader), "end_metric", SASErrorID.InvalidSectionEnd);
        }

        /// <summary>
        /// Processes the SAS+ variables block.
        /// </summary>
        /// <param name="reader">Reader of the SAS+ input file.</param>
        private static void ProcessVariables(StreamReader reader)
        {
            int variablesCount = ParseAndCheckNum(GetNextLine(reader));

            for (int i = 0; i < variablesCount; i++)
            {
                CheckLine(GetNextLine(reader), "begin_variable", SASErrorID.InvalidSectionStart);

                string varName = GetNextLine(reader);
                int varAxiomLayer = ParseAndCheckNum(GetNextLine(reader), NumType.UnsignedIntegerOrMinusOne);
                int varDomainRange = ParseAndCheckNum(GetNextLine(reader));
                List<string> valuesMeaning = new List<string>();

                if (varDomainRange == 0)
                    throw new SASInputDataLoaderException(SASErrorID.InvalidVariableDomRange, lineNumber);

                for (int j = 0; j < varDomainRange; j++)
                {
                    string valueSymbolicName = GetNextLine(reader);
                    valuesMeaning.Add(valueSymbolicName);
                }

                loadedInputData.Variables.Add(new SASInputData.Variable(varName, varAxiomLayer, varDomainRange, valuesMeaning));

                CheckLine(GetNextLine(reader), "end_variable", SASErrorID.InvalidSectionEnd);
            }
        }

        /// <summary>
        /// Processes the SAS+ mutexes block.
        /// </summary>
        /// <param name="reader">Reader of the SAS+ input file.</param>
        private static void ProcessMutexGroups(StreamReader reader)
        {
            int mutexGroupsCount = ParseAndCheckNum(GetNextLine(reader));

            for (int i = 0; i < mutexGroupsCount; i++)
            {
                CheckLine(GetNextLine(reader), "begin_mutex_group", SASErrorID.InvalidSectionStart);

                List<SASInputData.VariableValuePair> mutexGroup = new List<SASInputData.VariableValuePair>();

                int mutexSize = ParseAndCheckNum(GetNextLine(reader));
                for (int j = 0; j < mutexSize; j++)
                {
                    var numList = ParseAndCheckNumList(GetNextLine(reader), NumListType.VariableValuePair);
                    CheckDefinedVariable(numList[0]);
                    CheckValueForVariable(numList[0], numList[1]);

                    SASInputData.VariableValuePair newMutexItem = new SASInputData.VariableValuePair(numList[0], numList[1]);

                    if (mutexGroup.Contains(newMutexItem))
                        throw new SASInputDataLoaderException(SASErrorID.DuplicateItemsInMutexGroup, lineNumber,
                            new string []{ i.ToString(), numList[0].ToString(), numList[1].ToString() });

                    mutexGroup.Add(newMutexItem);
                }

                if (mutexGroup.Count > 0)
                    loadedInputData.MutexGroups.Add(new SASInputData.MutexGroup(mutexGroup));

                CheckLine(GetNextLine(reader), "end_mutex_group", SASErrorID.InvalidSectionEnd);
            }
        }

        /// <summary>
        /// Processes the SAS+ initial state block.
        /// </summary>
        /// <param name="reader">Reader of the SAS+ input file.</param>
        private static void ProcessInitState(StreamReader reader)
        {
            CheckLine(GetNextLine(reader), "begin_state", SASErrorID.InvalidSectionStart);

            List<int> initialStateValues = new List<int>();
            for (int i = 0; i < loadedInputData.Variables.Count; ++i)
            {
                int value = ParseAndCheckNum(GetNextLine(reader));
                CheckValueForVariable(i, value);

                initialStateValues.Add(value);
            }

            loadedInputData.InitState = new SASInputData.InitialState(initialStateValues);

            CheckMutexGroupsWithInitState();

            CheckLine(GetNextLine(reader), "end_state", SASErrorID.InvalidSectionEnd);
        }

        /// <summary>
        /// Checks whether the loaded initial state complies with defined mutex constraints.
        /// </summary>
        private static void CheckMutexGroupsWithInitState()
        {
            for (int groupIdx = 0; groupIdx < loadedInputData.MutexGroups.Count; ++groupIdx)
            {
                var mutexGroupData = loadedInputData.MutexGroups[groupIdx];
                int lockedItemIdx = -1;

                for (int itemIdx = 0; itemIdx < mutexGroupData.Contraints.Count; ++itemIdx)
                {
                    var mutexItem = mutexGroupData.Contraints[itemIdx];
                    if (loadedInputData.InitState.StateValues[mutexItem.Variable] == mutexItem.Value)
                    {
                        if (lockedItemIdx != -1)
                            throw new SASInputDataLoaderException(SASErrorID.InitStateInvalidToMutexes, lineNumber);
                        lockedItemIdx = itemIdx;
                    }
                }
            }
        }

        /// <summary>
        /// Processes the SAS+ goal state block.
        /// </summary>
        /// <param name="reader">Reader of the SAS+ input file.</param>
        private static void ProcessGoalState(StreamReader reader)
        {
            CheckLine(GetNextLine(reader), "begin_goal", SASErrorID.InvalidSectionStart);

            List<SASInputData.VariableValuePair> goalCondPairs = new List<SASInputData.VariableValuePair>();

            int goalConditionsCount = ParseAndCheckNum(GetNextLine(reader));

            for (int i = 0; i < goalConditionsCount; i++)
            {
                var numList = ParseAndCheckNumList(GetNextLine(reader), NumListType.VariableValuePair);
                CheckDefinedVariable(numList[0]);
                CheckValueForVariable(numList[0], numList[1]);

                goalCondPairs.Add(new SASInputData.VariableValuePair(numList[0], numList[1]));
            }

            loadedInputData.GoalConds = new SASInputData.GoalConditions(goalCondPairs);

            CheckLine(GetNextLine(reader), "end_goal", SASErrorID.InvalidSectionEnd);
        }

        /// <summary>
        /// Processes the SAS+ operators block.
        /// </summary>
        /// <param name="reader">Reader of the SAS+ input file.</param>
        private static void ProcessOperators(StreamReader reader)
        {
            int operatorsCount = ParseAndCheckNum(GetNextLine(reader));

            for (int i = 0; i < operatorsCount; i++)
            {
                CheckLine(GetNextLine(reader), "begin_operator", SASErrorID.InvalidSectionStart);

                string opName = GetNextLine(reader);
                List<SASInputData.Operator.Precondition> opConditions = new List<SASInputData.Operator.Precondition>();

                int prevailConditionsCount = ParseAndCheckNum(GetNextLine(reader));
                for (int j = 0; j < prevailConditionsCount; j++)
                {
                    var numList = ParseAndCheckNumList(GetNextLine(reader), NumListType.VariableValuePair);
                    CheckDefinedVariable(numList[0]);
                    CheckValueForVariable(numList[0], numList[1]);

                    opConditions.Add(new SASInputData.Operator.Precondition(numList[0], numList[1]));
                }

                List<SASInputData.Operator.Effect> opEffects = new List<SASInputData.Operator.Effect>();

                int effectsCount = ParseAndCheckNum(GetNextLine(reader));
                for (int j = 0; j < effectsCount; j++)
                {
                    var effParamList = ParseAndCheckNumList(GetNextLine(reader), NumListType.OperatorEffect);

                    int conditionsCount = effParamList[0];
                    List<SASInputData.Operator.Precondition> condEffConditions = new List<SASInputData.Operator.Precondition>();

                    int paramIdx = 1;

                    for (int effCond = 0; effCond < conditionsCount; ++effCond)
                    {
                        int condEffVar = effParamList[paramIdx++];
                        int condEffVal = effParamList[paramIdx++];
                        CheckDefinedVariable(condEffVar);
                        CheckValueForVariable(condEffVar, condEffVal);

                        condEffConditions.Add(new SASInputData.Operator.Precondition(condEffVar, condEffVal));
                    }

                    int effVar = effParamList[paramIdx++];
                    int effOldVal = effParamList[paramIdx++];
                    int effNewVal = effParamList[paramIdx++];

                    CheckDefinedVariable(effVar);
                    CheckValueForVariable(effVar, effNewVal);
                    CheckOperatorAffectedVariable(effVar);

                    if (effOldVal != -1)
                    {
                        CheckValueForVariable(effVar, effOldVal);
                        opConditions.Add(new SASInputData.Operator.Precondition(effVar, effOldVal));
                    }

                    opEffects.Add(new SASInputData.Operator.Effect(condEffConditions, effVar, effNewVal));
                }

                int opCost = ParseAndCheckNum(GetNextLine(reader));
                if (loadedInputData.IsMetricUsed == false || opCost == 0)
                    opCost = 1;

                loadedInputData.Operators.Add(new SASInputData.Operator(opName, opConditions, opEffects, opCost));

                CheckLine(GetNextLine(reader), "end_operator", SASErrorID.InvalidSectionEnd);
            }
        }

        /// <summary>
        /// Checks whether the given variable can be affected by an operator. If not, throws SASInputDataLoaderException.
        /// </summary>
        /// <param name="varID">Variable ID.</param>
        private static void CheckOperatorAffectedVariable(int varID)
        {
            // if the variable has axiom layer defined, then it can be affected only by axiom rules
            if (loadedInputData.Variables[varID].AxiomLayer != -1)
                throw new SASInputDataLoaderException(SASErrorID.VarCannotBeAffectedByOperator, lineNumber, varID.ToString());
        }

        /// <summary>
        /// Processes the SAS+ axioms block.
        /// </summary>
        /// <param name="reader">Reader of the SAS+ input file.</param>
        private static void ProcessAxiomRules(StreamReader reader)
        {
            int axiomsCount = ParseAndCheckNum(GetNextLine(reader));

            for (int i = 0; i < axiomsCount; i++)
            {
                List<SASInputData.VariableValuePair> axiomConditions = new List<SASInputData.VariableValuePair>();

                CheckLine(GetNextLine(reader), "begin_rule", SASErrorID.InvalidSectionStart);

                int conditionsCount = ParseAndCheckNum(GetNextLine(reader));
                for (int j = 0; j < conditionsCount; j++)
                {
                    var numList = ParseAndCheckNumList(GetNextLine(reader), NumListType.VariableValuePair);
                    CheckDefinedVariable(numList[0]);
                    CheckValueForVariable(numList[0], numList[1]);

                    axiomConditions.Add(new SASInputData.VariableValuePair(numList[0], numList[1]));
                }

                var ruleHeadList = ParseAndCheckNumList(GetNextLine(reader), NumListType.AxiomRuleHead);
                int var = ruleHeadList[0];
                int oldValue = ruleHeadList[1];
                int newValue = ruleHeadList[2];

                CheckDefinedVariable(var);
                CheckValueForVariable(var, newValue);
                CheckAxiomAffectedVariable(var);

                SASInputData.VariableValuePair axiomEffect = new SASInputData.VariableValuePair(var, newValue);

                if (oldValue != -1)
                {
                    CheckValueForVariable(var, oldValue);
                    axiomConditions.Add(new SASInputData.VariableValuePair(var, oldValue));
                }
                
                loadedInputData.Axioms.Add(new SASInputData.AxiomRule(axiomConditions, axiomEffect));

                CheckLine(GetNextLine(reader), "end_rule", SASErrorID.InvalidSectionEnd);
            }
        }

        /// <summary>
        /// Checks whether the given variable can be affected by an axiom rule. If not, throws SASInputDataLoaderException.
        /// </summary>
        /// <param name="varID">Variable ID.</param>
        private static void CheckAxiomAffectedVariable(int varID)
        {
            // if the variable hasn't axiom layer defined, then it cannot be affected by axiom rules
            if (loadedInputData.Variables[varID].AxiomLayer == -1)
                throw new SASInputDataLoaderException(SASErrorID.VarCannotBeAffectedByAxiomRule, lineNumber, varID.ToString());
        }

    }
}
