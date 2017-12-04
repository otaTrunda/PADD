using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Auxiliary static class for creation of PDDLProblem instance from input files.
    /// </summary>
    public static class PDDLProblemLoader
    {
        /// <summary>
        /// Creates PDDL planning problem instance from input files.
        /// </summary>
        /// <param name="domainFile">PDDL domain file.</param>
        /// <param name="problemFile">PDDL problem file.</param>
        /// <param name="designatorFactory">PDDL designator factory. If not specified, default designator implementation is used.</param>
        /// <param name="stateFactory">PDDL state factory. If not specified, default state implementation is used.</param>
        /// <returns>New instance of PDDL planning problem.</returns>
        public static PDDLProblem CreateProblemInstanceFromFiles(string domainFile, string problemFile, IPDDLDesignatorFactory designatorFactory = null, IPDDLStateFactory stateFactory = null)
        {
            ResetContainers();
            SetDesignatorFactory(designatorFactory);

            ReadFile(FileType.Domain, domainFile);
            ReadFile(FileType.Problem, problemFile);

            return new PDDLProblem(domainName, problemName, initialPredicates, initialFunctions,
                goalConditions, opNames, opInputParams, opPreconds, opEffects, idManager, stateFactory);
        }

        /// <summary>
        /// Name of the read PDDL domain.
        /// </summary>
        private static string domainName;

        /// <summary>
        /// Name of the read PDDL problem.
        /// </summary>
        private static string problemName;

        /// <summary>
        /// Initial predicates of the read PDDL problem.
        /// </summary>
        private static List<IPDDLDesignator> initialPredicates;

        /// <summary>
        /// Initial function values of the read PDDL problem.
        /// </summary>
        private static Dictionary<IPDDLDesignator, int> initialFunctions;

        /// <summary>
        /// Goal conditions of the read PDDL problem.
        /// </summary>
        private static IPDDLLogicalExpression goalConditions;

        /// <summary>
        /// Container for read operator names.
        /// </summary>
        private static List<string> opNames;

        /// <summary>
        /// Container for read operator input parameters.
        /// </summary>
        private static List<PDDLOperatorLifted.InputParams> opInputParams;

        /// <summary>
        /// Container for read operator preconditions.
        /// </summary>
        private static List<PDDLOperatorLifted.Preconditions> opPreconds;

        /// <summary>
        /// Container for read operator effects.
        /// </summary>
        private static List<PDDLOperatorLifted.Effects> opEffects;

        /// <summary>
        /// Manager for the ID mappings in the read PDDL planning problem.
        /// </summary>
        private static PDDLIdentifierMappingsManager idManager;

        /// <summary>
        /// Factory for the creation of PDDLDesignator instances.
        /// </summary>
        private static IPDDLDesignatorFactory designatorFactory;

        /// <summary>
        /// Resets all of the containers for reading PDDL input files.
        /// </summary>
        private static void ResetContainers()
        {
            domainName = "";
            problemName = "";

            initialPredicates = new List<IPDDLDesignator>();
            initialFunctions = new Dictionary<IPDDLDesignator, int>();

            goalConditions = null;

            opNames = new List<string>();
            opInputParams = new List<PDDLOperatorLifted.InputParams>();
            opPreconds = new List<PDDLOperatorLifted.Preconditions>();
            opEffects = new List<PDDLOperatorLifted.Effects>();

            idManager = new PDDLIdentifierMappingsManager();
        }

        /// <summary>
        /// Sets the PDDLDesignator factory, used for the creation of PDDLDesignator instances.
        /// </summary>
        /// <param name="designatorFactory">PDDLDesignator factory.</param>
        private static void SetDesignatorFactory(IPDDLDesignatorFactory designatorFactory)
        {
            if (designatorFactory == null)
                designatorFactory = new PDDLDesignatorFactory();

            PDDLProblemLoader.designatorFactory = designatorFactory;
        }

        /// <summary>
        /// PDDL input file types.
        /// </summary>
        private enum FileType
        {
            Domain,
            Problem
        }

        /// <summary>
        /// PDDL domain requirements.
        /// </summary>
        private enum DomainRequirements
        {
            Typing,
            Equality,
            NegativePrecond,
            DisjunctivePrecond,
            ExistentialPrecond,
            UniversalPrecond,
            ConditionalEffects,
            NumericFluents,
            ObjectFluents,
            ActionCosts
        }

        /// <summary>
        /// Active domain requirements.
        /// </summary>
        private static DomainRequirements[] requirements = new DomainRequirements[0];

        /// <summary>
        /// Auxiliary struct for fetching data from the PDDL input files.
        /// </summary>
        private struct FetchedBlock
        {
            /// <summary>
            /// Header of the fetched block.
            /// </summary>
            public string blockHeader;

            /// <summary>
            /// Body of the fetched block.
            /// </summary>
            public string blockBody;

            /// <summary>
            /// Rest data after the fetched block (to be processed).
            /// </summary>
            public string rest;
        }

        /// <summary>
        /// Reads the data of a PDDL input file to the containers.
        /// </summary>
        /// <param name="fileType">Input file type.</param>
        /// <param name="fileName">Input file name.</param>
        private static void ReadFile(FileType fileType, string fileName)
        {
            using (var reader = new System.IO.StreamReader(fileName))
            {
                string str = reader.ReadToEnd();

                str = CleanFromComments(str);
                FetchedBlock block = FetchNextBlock(str);

                if (block.blockHeader != "define")
                    throw new PDDLProblemLoaderException(PDDLErrorID.FileNotStartWithDefineBlock);

                if (block.rest.Length != 0)
                {
                    FetchedBlock restBlock = FetchNextBlock(block.rest);
                    if (restBlock.blockHeader == "define")
                        throw new PDDLProblemLoaderException(PDDLErrorID.FileHasMoreDefineBlocks, (fileType == FileType.Domain) ? "domain" : "problem");
                    else
                        throw new PDDLProblemLoaderException(PDDLErrorID.FileHasBadBracketing);
                }

                ProcessDefine(block.blockBody, fileType);
            }
        }

        /// <summary>
        /// Cleans the input PDDL definition string from comments.
        /// </summary>
        /// <param name="str">PDDL definition string.</param>
        /// <returns>Input string without comments.</returns>
        private static string CleanFromComments(string str)
        {
            StringBuilder sb = new StringBuilder();

            bool inComment = false;
            for (int i = 0; i < str.Length; ++i)
            {
                char currChar = str[i];
                switch (currChar)
                {
                    case ';':
                        inComment = true;
                        break;
                    case '\n':
                        inComment = false;
                        break;
                    default:
                        if (!inComment)
                            sb.Append(currChar);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Fetches the next data block.
        /// </summary>
        /// <param name="str">Input definition string.</param>
        /// <param name="withoutHeader">Should the block header be ignored?</param>
        /// <returns>Fetched data block.</returns>
        private static FetchedBlock FetchNextBlock(string str, bool withoutHeader = false)
        {
            str = str.Trim();

            if (str.Length == 0)
                return new FetchedBlock();

            if (str[0] != '(')
            {
                if (str[0] == '?')
                {
                    int idxDelim = Array.FindIndex(str.ToCharArray(), x => (char.IsWhiteSpace(x) || x == '('));
                    throw new PDDLProblemLoaderException(PDDLErrorID.ExprInvalidVariableOutsidePredicate, (idxDelim != -1) ? str.Substring(0, idxDelim) : "??");
                }
                throw new PDDLProblemLoaderException(PDDLErrorID.FileHasBadBracketing);
            }

            int bracketLevel = 0;
            int endIdx = 0;

            for (int i = 0; i < str.Length; ++i)
            {
                if (str[i] == '(')
                    ++bracketLevel;
                else if (str[i] == ')')
                    --bracketLevel;

                if (bracketLevel == 0)
                {
                    endIdx = i;
                    break;
                }
            }

            if (bracketLevel != 0)
                throw new PDDLProblemLoaderException(PDDLErrorID.FileHasBadBracketing);

            FetchedBlock fetchedBlock = new FetchedBlock();

            string currBlock = str.Substring(1, endIdx - 1).Trim(); // remove first and last bracket

            if (!withoutHeader)
            {
                int idxDelim = Array.FindIndex(currBlock.ToCharArray(), x => (char.IsWhiteSpace(x) || x == '('));
                fetchedBlock.blockHeader = (idxDelim != -1) ? currBlock.Substring(0, idxDelim).Trim() : currBlock;
                fetchedBlock.blockBody = (idxDelim != -1) ? currBlock.Substring(idxDelim).Trim() : "";
            }
            else
            {
                fetchedBlock.blockBody = currBlock;
            }

            fetchedBlock.rest = str.Substring(endIdx + 1).Trim();

            return fetchedBlock;
        }

        /// <summary>
        /// Checks the correctness of PDDL name - correct name: (first char letter) + ( a-z | A-Z | 0-9 | - | _ )*.
        /// Variable additionally starts with '?'.
        /// </summary>
        /// <param name="str">String to be checked.</param>
        /// <param name="isVar">Check variable name.</param>
        /// <returns>True if the specified name is correct, false otherwise.</returns>
        private static bool CheckName(string str, bool isVar = false)
        {
            if (str.Length == 0)
                return false;

            if (isVar)
            {
                if (str[0] == '?' && str.Length > 1)
                    str = str.Remove(0, 1);
                else
                    return false;
            }

            string pattern = @"^[a-zA-Z][a-zA-Z0-9_-]*$";
            return System.Text.RegularExpressions.Regex.IsMatch(str, pattern);
        }

        /// <summary>
        /// Checks whether the specified string is not a reserved keywords. Predicate/function name cannot be
        /// one of the specified keywords: and, not, or, imply, exists, forall, when, increase, decrease, assign.
        /// </summary>
        /// <param name="str">String to be checked.</param>
        /// <returns>True if the specified name is not a reserved keyword, false otherwise.</returns>
        private static bool CheckPredOrFuncNameForKeywords(string str)
        {
            return (GetExpressionToken(str) == ExprToken.PREDICATE);
        }

        /// <summary>
        /// Segments of the input PDDL domain file.
        /// </summary>
        private enum DomainSegments
        {
            DomainName,
            Requirements,
            Types,
            Constants,
            Predicates,
            Functions,
            Action
        }

        /// <summary>
        /// Segments of the input PDDL problem file.
        /// </summary>
        private enum ProblemSegments
        {
            ProblemName,
            MappedDomain,
            Objects,
            Init,
            Goal
        }

        /// <summary>
        /// Processes the PDDL ':define' block.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <param name="fileType">Input file type.</param>
        private static void ProcessDefine(string str, FileType fileType)
        {
            HashSet<DomainSegments> processedDomainSegments = new HashSet<DomainSegments>();
            HashSet<ProblemSegments> processedProblemSegments = new HashSet<ProblemSegments>();

            while (str.Length != 0)
            {
                FetchedBlock currBlock = FetchNextBlock(str);
                str = currBlock.rest;

                if (fileType == FileType.Domain)
                {
                    switch (currBlock.blockHeader)
                    {
                        case "domain":
                            if (processedDomainSegments.Contains(DomainSegments.DomainName))
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineDomainMultipleDomainNames);
                            else if (processedDomainSegments.Count != 0)
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineDomainNameMissing);
                            domainName = currBlock.blockBody;
                            processedDomainSegments.Add(DomainSegments.DomainName);
                            break;

                        case ":requirements":
                            if (processedDomainSegments.Contains(DomainSegments.Requirements))
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineDomainDuplicateSegments, currBlock.blockHeader);
                            ProcessRequirements(currBlock.blockBody);
                            processedDomainSegments.Add(DomainSegments.Requirements);
                            break;

                        case ":types":
                            if (processedDomainSegments.Contains(DomainSegments.Types))
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineDomainDuplicateSegments, currBlock.blockHeader);
                            ProcessTypes(currBlock.blockBody);
                            processedDomainSegments.Add(DomainSegments.Types);
                            break;

                        case ":constants":
                            if (processedDomainSegments.Contains(DomainSegments.Constants))
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineProblemDuplicateSegments, currBlock.blockHeader);
                            ProcessConstants(currBlock.blockBody);
                            processedDomainSegments.Add(DomainSegments.Constants);
                            break;

                        case ":predicates":
                            if (processedDomainSegments.Contains(DomainSegments.Predicates))
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineDomainDuplicateSegments, currBlock.blockHeader);
                            ProcessPredicates(currBlock.blockBody);
                            processedDomainSegments.Add(DomainSegments.Predicates);
                            break;

                        case ":functions":
                            if (processedDomainSegments.Contains(DomainSegments.Functions))
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineDomainDuplicateSegments, currBlock.blockHeader);
                            ProcessFunctions(currBlock.blockBody);
                            processedDomainSegments.Add(DomainSegments.Functions);
                            break;

                        case ":action":
                            ProcessAction(currBlock.blockBody);
                            processedDomainSegments.Add(DomainSegments.Action);
                            break;

                        default:
                            throw new PDDLProblemLoaderException(PDDLErrorID.InvalidDomainBlock, currBlock.blockHeader);
                    }
                }
                else // FileType.PROBLEM
                {
                    switch (currBlock.blockHeader)
                    {
                        case "problem":
                            if (processedProblemSegments.Contains(ProblemSegments.ProblemName))
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineProblemMultipleProblemNames);
                            else if (processedProblemSegments.Count != 0)
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineProblemNameMissing);
                            problemName = currBlock.blockBody;
                            processedProblemSegments.Add(ProblemSegments.ProblemName);
                            break;

                        case ":domain":
                            if (processedProblemSegments.Contains(ProblemSegments.MappedDomain))
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineProblemDuplicateSegments, currBlock.blockHeader);
                            if (domainName != currBlock.blockBody)
                                throw new PDDLProblemLoaderException(PDDLErrorID.DomainProblemMismatch);
                            processedProblemSegments.Add(ProblemSegments.MappedDomain);
                            break;

                        case ":objects":
                            if (processedProblemSegments.Contains(ProblemSegments.Objects))
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineProblemDuplicateSegments, currBlock.blockHeader);
                            ProcessObjects(currBlock.blockBody);
                            processedProblemSegments.Add(ProblemSegments.Objects);
                            break;

                        case ":init":
                            if (processedProblemSegments.Contains(ProblemSegments.Init))
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineProblemDuplicateSegments, currBlock.blockHeader);
                            ProcessInit(currBlock.blockBody);
                            processedProblemSegments.Add(ProblemSegments.Init);
                            break;

                        case ":goal":
                            if (processedProblemSegments.Contains(ProblemSegments.Goal))
                                throw new PDDLProblemLoaderException(PDDLErrorID.DefineProblemDuplicateSegments, currBlock.blockHeader);
                            ProcessGoal(currBlock.blockBody);
                            processedProblemSegments.Add(ProblemSegments.Goal);
                            break;

                        default:
                            throw new PDDLProblemLoaderException(PDDLErrorID.InvalidProblemBlock, currBlock.blockHeader);
                    }
                }
            }

            if (fileType == FileType.Domain)
            {
                if (!processedDomainSegments.Contains(DomainSegments.DomainName))
                    throw new PDDLProblemLoaderException(PDDLErrorID.DefineDomainNameMissing);
                else if (!processedDomainSegments.Contains(DomainSegments.Action))
                    throw new PDDLProblemLoaderException(PDDLErrorID.DefineDomainNoAction);
            }
            else // (fileType == FileType.PROBLEM)
            {
                if (!processedProblemSegments.Contains(ProblemSegments.ProblemName))
                    throw new PDDLProblemLoaderException(PDDLErrorID.DefineProblemNameMissing);
                else if (!processedProblemSegments.Contains(ProblemSegments.MappedDomain))
                    throw new PDDLProblemLoaderException(PDDLErrorID.DefineProblemMissingMappedDomain);
                else if (!processedProblemSegments.Contains(ProblemSegments.Init))
                    throw new PDDLProblemLoaderException(PDDLErrorID.DefineProblemMissingInit);
                else if (!processedProblemSegments.Contains(ProblemSegments.Goal))
                    throw new PDDLProblemLoaderException(PDDLErrorID.DefineProblemMissingGoal);
            }
        }

        /// <summary>
        /// Processes the PDDL ':requirements' block.
        /// </summary>
        /// <param name="str">Input string.</param>
        private static void ProcessRequirements(string str)
        {
            List<DomainRequirements> reqList = new List<DomainRequirements>();

            string[] reqs = str.Split(new char[0], StringSplitOptions.RemoveEmptyEntries); // split by whitespaces
            foreach (var req in reqs)
            {
                switch (req)
                {
                    case ":strips":
                        break;
                    case ":typing":
                        reqList.Add(DomainRequirements.Typing);
                        break;
                    case ":equality":
                        reqList.Add(DomainRequirements.Equality);
                        break;
                    case ":negative-preconditions":
                        reqList.Add(DomainRequirements.NegativePrecond);
                        break;
                    case ":disjunctive-preconditions":
                        reqList.Add(DomainRequirements.DisjunctivePrecond);
                        break;
                    case ":existential-preconditions":
                        reqList.Add(DomainRequirements.ExistentialPrecond);
                        break;
                    case ":universal-preconditions":
                        reqList.Add(DomainRequirements.UniversalPrecond);
                        break;
                    case ":quantified-preconditions":
                        reqList.Add(DomainRequirements.ExistentialPrecond);
                        reqList.Add(DomainRequirements.UniversalPrecond);
                        break;
                    case ":conditional-effects":
                        reqList.Add(DomainRequirements.ConditionalEffects);
                        break;
                    case ":adl":
                        reqList.Add(DomainRequirements.Typing);
                        reqList.Add(DomainRequirements.NegativePrecond);
                        reqList.Add(DomainRequirements.DisjunctivePrecond);
                        reqList.Add(DomainRequirements.Equality);
                        reqList.Add(DomainRequirements.ExistentialPrecond);
                        reqList.Add(DomainRequirements.UniversalPrecond);
                        reqList.Add(DomainRequirements.ConditionalEffects);
                        break;
                    case ":numeric-fluents":
                        reqList.Add(DomainRequirements.NumericFluents);
                        break;
                    case ":object-fluents":
                        reqList.Add(DomainRequirements.ObjectFluents);
                        break;
                    case ":fluents":
                        reqList.Add(DomainRequirements.NumericFluents);
                        reqList.Add(DomainRequirements.ObjectFluents);
                        break;
                    case ":action-costs":
                        reqList.Add(DomainRequirements.ActionCosts);
                        break;
                    default:
                        throw new PDDLProblemLoaderException(PDDLErrorID.UnsupportedRequirement, req);
                }
            }

            requirements = reqList.ToArray();
        }

        /// <summary>
        /// Processes the PDDL ':types' block.
        /// </summary>
        /// <param name="str">Input string.</param>
        private static void ProcessTypes(string str)
        {
            if (!requirements.Contains(DomainRequirements.Typing))
                throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqTypingForTypesDef);

            List<Tuple<string, int, int, string>> objectList = new List<Tuple<string, int, int, string>>();
            ProcessTypedList(str, objectList, TokenType.Type);

            foreach (var obj in objectList)
            {
                string typeName = obj.Item1;

                if (!CheckName(typeName))
                    throw new PDDLProblemLoaderException(PDDLErrorID.InvalidNameForType, typeName);
                else if (typeName == "object")
                    throw new PDDLProblemLoaderException(PDDLErrorID.InvalidTypeRedefinitionObject);
                else if (typeName == "number" && requirements.Contains(DomainRequirements.NumericFluents))
                    throw new PDDLProblemLoaderException(PDDLErrorID.InvalidTypeRedefinitionNumber);

                if (!idManager.GetTypesMapping().SetType(typeName, obj.Item4))
                    throw new PDDLProblemLoaderException(PDDLErrorID.InvalidTypeRedefinition, typeName);
            }
        }

        /// <summary>
        /// Processes the PDDL ':constants' block.
        /// </summary>
        /// <param name="str">Input string.</param>
        private static void ProcessConstants(string str)
        {
            var constList = new List<Tuple<string, int, int, string>>();
            ProcessTypedList(str, constList, TokenType.Constant);

            foreach (var constt in constList)
            {
                if (!CheckName(constt.Item1))
                    throw new PDDLProblemLoaderException(PDDLErrorID.InvalidNameForConstant, constt.Item1);
                idManager.GetConstantsMapping().SetConst(constt.Item1, constt.Item3);
            }
        }

        /// <summary>
        /// Processes the PDDL ':predicates' block.
        /// </summary>
        /// <param name="str">Input string.</param>
        private static void ProcessPredicates(string str)
        {
            while (str.Length != 0)
            {
                FetchedBlock currBlock = FetchNextBlock(str);
                str = currBlock.rest;

                List<Tuple<string, int, int, string>> varList = new List<Tuple<string, int, int, string>>(); // varName:<dontCare>:typeID

                ProcessTypedList(currBlock.blockBody, varList, TokenType.Variable);

                string predName = currBlock.blockHeader;

                if (!CheckName(predName))
                    throw new PDDLProblemLoaderException(PDDLErrorID.InvalidNameForPredicate, predName);
                if (!CheckPredOrFuncNameForKeywords(predName))
                    throw new PDDLProblemLoaderException(PDDLErrorID.PredOrFuncNameIsKeyword, predName);

                int[] paramTypeIDs = new int[varList.Count];
                for (int i = 0; i < paramTypeIDs.Length; ++i)
                {
                    if (!CheckName(varList[i].Item1, true))
                        throw new PDDLProblemLoaderException(PDDLErrorID.InvalidNameForVariable, varList[i].Item1);
                    paramTypeIDs[i] = varList[i].Item3;
                }

                if (!idManager.GetPredicatesMapping().SetPredicate(predName, paramTypeIDs))
                    throw new PDDLProblemLoaderException(PDDLErrorID.DuplicatePredicateDefined, predName);
                if (idManager.GetFunctionsMapping().GetFunctionID(predName) != -1)
                    throw new PDDLProblemLoaderException(PDDLErrorID.PredicateNameUsedForFunction, predName);
            }
        }

        /// <summary>
        /// Processes the PDDL ':functions' block.
        /// </summary>
        /// <param name="str">Input string.</param>
        private static void ProcessFunctions(string str)
        {
            List<Tuple<string, List<int>>> funcList = new List<Tuple<string, List<int>>>();
            bool anyObjFluent = false;
            bool anyActionCostFluent = false;
            int countNumFluents = 0;

            bool isActionCostsReq = requirements.Contains(DomainRequirements.ActionCosts);

            while (str.Length != 0)
            {
                if (str[0] == '-')
                {
                    int nextBlockIdx = str.IndexOf('(');
                    if (nextBlockIdx == -1)
                        nextBlockIdx = str.Length;
                    string typeStr = str.Substring(1, nextBlockIdx - 1).Trim(); // remove '-'

                    int typeID = idManager.GetTypesMapping().GetTypeIDForFunction(typeStr);
                    if (typeID == -1)
                        throw new PDDLProblemLoaderException(PDDLErrorID.TypedListUnknownType, typeStr);
                    if (typeID == idManager.GetTypesMapping().GetNumericTypeID())
                        ++countNumFluents;
                    else
                        anyObjFluent = true;

                    foreach (var func in funcList)
                    {
                        string functionName = func.Item1;

                        if (isActionCostsReq && !anyActionCostFluent)
                            anyActionCostFluent = (functionName == "total-cost");
                        if (isActionCostsReq && functionName == "total-cost" && (func.Item2.Count != 0 || typeID != idManager.GetTypesMapping().GetNumericTypeID()))
                            throw new PDDLProblemLoaderException(PDDLErrorID.ActionCostsInvalidFunctionSpec);
                        if (!idManager.GetFunctionsMapping().SetFunction(functionName, func.Item2.ToArray(), typeID))
                            throw new PDDLProblemLoaderException(PDDLErrorID.DuplicateFunctionDefined, functionName);
                        if (idManager.GetPredicatesMapping().GetPredicateID(functionName) != -1)
                            throw new PDDLProblemLoaderException(PDDLErrorID.FunctionNameUsedForPredicate, functionName);
                    }
                    funcList.Clear();

                    str = str.Substring(nextBlockIdx).Trim();
                    continue;
                }

                FetchedBlock currBlock = FetchNextBlock(str);
                str = currBlock.rest.Trim();

                List<Tuple<string, int, int, string>> varList = new List<Tuple<string, int, int, string>>(); // varName:<dontCare>:typeID:<dontCare>

                ProcessTypedList(currBlock.blockBody, varList, TokenType.Variable);

                string funcName = currBlock.blockHeader;

                if (!CheckName(funcName))
                    throw new PDDLProblemLoaderException(PDDLErrorID.InvalidNameForFunction, funcName);
                if (!CheckPredOrFuncNameForKeywords(funcName))
                    throw new PDDLProblemLoaderException(PDDLErrorID.PredOrFuncNameIsKeyword, funcName);

                List<int> paramTypeIDs = new List<int>();
                for (int i = 0; i < varList.Count; ++i)
                {
                    if (!CheckName(varList[i].Item1, true))
                        throw new PDDLProblemLoaderException(PDDLErrorID.InvalidNameForVariable, varList[i].Item1);
                    paramTypeIDs.Add(varList[i].Item3);
                }

                funcList.Add(Tuple.Create(funcName, paramTypeIDs));
            }

            if (funcList.Count != 0)
            {
                foreach (var func in funcList)
                {
                    string functionName = func.Item1;
                    int typeID = idManager.GetTypesMapping().GetNumericTypeID(); // default fluent type is 'number'

                    if (isActionCostsReq && !anyActionCostFluent)
                        anyActionCostFluent = (functionName == "total-cost");
                    if (isActionCostsReq && functionName == "total-cost" && func.Item2.Count != 0)
                        throw new PDDLProblemLoaderException(PDDLErrorID.ActionCostsInvalidFunctionSpec);
                    if (!idManager.GetFunctionsMapping().SetFunction(functionName, func.Item2.ToArray(), typeID))
                        throw new PDDLProblemLoaderException(PDDLErrorID.DuplicateFunctionDefined, functionName);
                    if (idManager.GetPredicatesMapping().GetPredicateID(functionName) != -1)
                        throw new PDDLProblemLoaderException(PDDLErrorID.FunctionNameUsedForPredicate, functionName);
                    ++countNumFluents;
                }
            }

            bool onlyPresentNumFluentIsActionCost = (isActionCostsReq && anyActionCostFluent && countNumFluents == 1);

            if (countNumFluents > 0 && !onlyPresentNumFluentIsActionCost && !requirements.Contains(DomainRequirements.NumericFluents))
                throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqNumFluentsForFuncDef);
            else if (anyObjFluent && !requirements.Contains(DomainRequirements.ObjectFluents))
                throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqObjFluentsForFuncDef);
        }

        /// <summary>
        /// Processes the PDDL ':action' block.
        /// </summary>
        /// <param name="str">Input string.</param>
        private static void ProcessAction(string str)
        {
            int idxName = str.IndexOf(' ');
            string opName = str.Substring(0, idxName).Trim();
            str = str.Substring(idxName + 1).Trim();

            if (!str.StartsWith(":parameters"))
                throw new PDDLProblemLoaderException(PDDLErrorID.ActionMissingParameters);

            str = str.Remove(0, 11).Trim(); // remove ":parameters"

            if (!str.StartsWith("("))
                throw new PDDLProblemLoaderException(PDDLErrorID.ActionEmptyParameters);

            FetchedBlock currBlock = FetchNextBlock(str, true);
            str = currBlock.rest.Trim();

            List<Tuple<string, int, int, string>> varList = new List<Tuple<string, int, int, string>>(); // varName:varID:typeID
            Dictionary<string, Tuple<int, int>> varNameToIDAndTypeMapping = new Dictionary<string, Tuple<int, int>>();

            ProcessTypedList(currBlock.blockBody, varList, TokenType.Variable);

            int[] paramsVarTypeIDs = new int[varList.Count];

            for (int i = 0; i < varList.Count; ++i)
            {
                if (!CheckName(varList[i].Item1, true))
                    throw new PDDLProblemLoaderException(PDDLErrorID.InvalidNameForVariable, varList[i].Item1);
                paramsVarTypeIDs[i] = varList[i].Item3;
                varNameToIDAndTypeMapping.Add(varList[i].Item1, Tuple.Create(varList[i].Item2, varList[i].Item3)); // varName:paramID:typeID
            }

            PDDLOperatorLifted.InputParams opParams = new PDDLOperatorLifted.InputParams(paramsVarTypeIDs);

            if (!str.StartsWith(":precondition"))
                throw new PDDLProblemLoaderException(PDDLErrorID.ActionMissingPreconditions);

            int effIdx = str.IndexOf(":effect");
            if (effIdx == -1)
                throw new PDDLProblemLoaderException(PDDLErrorID.ActionMissingEffects);

            string precondStr = str.Substring(0, effIdx - 1).Trim();
            precondStr = precondStr.Remove(0, 13); // remove ":precondition"

            string effectStr = str.Substring(effIdx).Trim();
            effectStr = effectStr.Remove(0, 7); // remove ":effect"

            if (precondStr.Trim().Length == 0)
                throw new PDDLProblemLoaderException(PDDLErrorID.ActionEmptyPreconditions);
            else if (effectStr.Trim().Length == 0)
                throw new PDDLProblemLoaderException(PDDLErrorID.ActionEmptyEffects);

            IPDDLLogicalExpression precondExpr = new ExpressionBuilder(varNameToIDAndTypeMapping, opParams.GetNumberOfParams(), ExprType.Precondition).BuildTree(precondStr);
            PDDLOperatorLifted.Preconditions preconds = new PDDLOperatorLifted.Preconditions(precondExpr);

            IPDDLLogicalExpression effectExpr = new ExpressionBuilder(varNameToIDAndTypeMapping, opParams.GetNumberOfParams(), ExprType.Effect).BuildTree(effectStr);
            PDDLOperatorLifted.Effects effects = new PDDLOperatorLifted.Effects(idManager, effectExpr);

            opNames.Add(opName);
            opInputParams.Add(opParams);
            opPreconds.Add(preconds);
            opEffects.Add(effects);
        }

        /// <summary>
        /// Processes the PDDL ':objects' block.
        /// </summary>
        /// <param name="str">Input string.</param>
        private static void ProcessObjects(string str)
        {
            List<Tuple<string, int, int, string>> objectList = new List<Tuple<string, int, int, string>>();
            ProcessTypedList(str, objectList, TokenType.Constant);

            foreach (var obj in objectList)
            {
                if (!CheckName(obj.Item1))
                    throw new PDDLProblemLoaderException(PDDLErrorID.InvalidNameForConstant, obj.Item1);
                idManager.GetConstantsMapping().SetConst(obj.Item1, obj.Item3);
            }
        }

        /// <summary>
        /// Specification of a token type - constant, variable or type.
        /// </summary>
        private enum TokenType
        {
            Constant,
            Variable,
            Type
        }

        /// <summary>
        /// Processes a generic typed list that is a part of other definition blocks.
        /// </summary>
        /// <param name="strTypedList">Input string.</param>
        /// <param name="outputList">Return list of tuples (paramStr, paramID, paramTypeID, typeStr).</param>
        /// <param name="tokenType">Token type of the typed list.</param>
        /// <param name="freeParamID">Explicit first free param ID.</param>
        private static void ProcessTypedList(string strTypedList, List<Tuple<string, int, int, string>> outputList, TokenType tokenType, int freeParamID = 0)
        {
            string[] paramList = strTypedList.Split(new char[0], StringSplitOptions.RemoveEmptyEntries); // whitespaces

            bool isVariableList = (tokenType == TokenType.Variable);
            bool isTypesList = (tokenType == TokenType.Type);

            List<string> currVarNames = new List<string>();
            bool nextShouldBeType = false;

            foreach (var par in paramList)
            {
                if (par.Length == 0)
                    continue;

                switch (par[0])
                {
                    case '?':
                        if (!isVariableList || nextShouldBeType || par.Length == 1)
                            throw new PDDLProblemLoaderException(PDDLErrorID.TypedListInvalidVariable);
                        currVarNames.Add(par.Trim());
                        break;
                    case '-':
                        if (!requirements.Contains(DomainRequirements.Typing))
                            throw new PDDLProblemLoaderException(PDDLErrorID.TypedListMissingTypingReq);
                        if (nextShouldBeType || currVarNames.Count == 0)
                            throw new PDDLProblemLoaderException(PDDLErrorID.TypedListInvalidTyping);
                        nextShouldBeType = true;
                        break;
                    default:
                        if (!nextShouldBeType)
                        {
                            if (isVariableList)
                                throw new PDDLProblemLoaderException(PDDLErrorID.TypedListInvalidConstant, par);
                            // current token is a constant - store it and continue
                            currVarNames.Add(par.Trim());
                            continue;
                        }

                        nextShouldBeType = false;
                        int typeIndex = idManager.GetTypesMapping().GetTypeID(par);
                        if (typeIndex == -1 && !isTypesList)
                            throw new PDDLProblemLoaderException(PDDLErrorID.TypedListUnknownType, par);

                        foreach (var strParam in currVarNames)
                            outputList.Add(Tuple.Create(strParam, freeParamID++, typeIndex, par));
                        currVarNames.Clear();
                        break;
                }
            }

            // the parameter list is over, store any pending parameters
            if (currVarNames.Count != 0)
            {
                if (requirements.Contains(DomainRequirements.Typing) && !isTypesList)
                    throw new PDDLProblemLoaderException(PDDLErrorID.TypedListMissingTyping);
                foreach (var strParam in currVarNames)
                {
                    outputList.Add(Tuple.Create(strParam, freeParamID++, 0, "object"));
                }
                currVarNames.Clear();
            }
        }

        /// <summary>
        /// Processes the PDDL ':init' block.
        /// </summary>
        /// <param name="str">Input string.</param>
        private static void ProcessInit(string str)
        {
            // correct ":init" block = list of predicates (and potentially (= funcA constA) assignments)

            bool totalCostAssigned = false;

            while (str.Length != 0)
            {
                FetchedBlock currBlock = FetchNextBlock(str);
                str = currBlock.rest;

                switch (GetExpressionToken(currBlock.blockHeader))
                {
                    case ExprToken.PREDICATE:

                        var pred = ExpressionBuilder.ProcessExprPredicateOrFunction(currBlock, ExpressionBuilder.PredFuncProcessingMode.PREDICATE_INIT, new Dictionary<string, Tuple<int, int>>());
                        initialPredicates.Add(designatorFactory.CreateDesignator(pred.Item1, pred.Item2));
                        break;

                    case ExprToken.EQUALS:

                        if (currBlock.blockBody.Length == 0 || currBlock.blockBody[0] != '(')
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprInitFluentAssignNotStartWithFunc);

                        FetchedBlock funcToAssign = FetchNextBlock(currBlock.blockBody);
                        string assignValStr = funcToAssign.rest.Trim();

                        var func = ExpressionBuilder.ProcessExprPredicateOrFunction(funcToAssign, ExpressionBuilder.PredFuncProcessingMode.FUNCTION_INIT, new Dictionary<string, Tuple<int, int>>());

                        if ((assignValStr.Length > 0 && assignValStr[0] == '?') || Array.FindIndex(assignValStr.ToCharArray(), x => (x == '(')) != -1)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprInitFluentAssignValueIsNotConst);

                        if (assignValStr.Length == 0 || Array.FindIndex(assignValStr.ToCharArray(), x => (char.IsWhiteSpace(x))) != -1)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprInitFluentAssignParamsCount);

                        int funcReturnType = idManager.GetFunctionsMapping().GetReturnType(funcToAssign.blockHeader);
                        int assignValType = -1;

                        int assignVal;
                        if (int.TryParse(assignValStr, out assignVal))
                        {
                            assignValType = idManager.GetTypesMapping().GetNumericTypeID();
                        }
                        else
                        {
                            assignVal = idManager.GetConstantsMapping().GetConstID(assignValStr);
                            if (assignVal == -1)
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprInitFluentAssignUnknownConstant, assignValStr);
                            assignValType = idManager.GetConstantsMapping().GetTypeID(assignValStr);
                        }

                        if (funcReturnType != assignValType)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprInitFluentAssignReturnType, assignValStr, funcToAssign.blockHeader);

                        if (requirements.Contains(DomainRequirements.ActionCosts) && func.Item1 == idManager.GetFunctionsMapping().GetFunctionID("total-cost"))
                        {
                            totalCostAssigned = true;
                            if (assignVal != 0)
                                throw new PDDLProblemLoaderException(PDDLErrorID.ActionCostsNotInitedToZero);
                        }

                        initialFunctions.Add(designatorFactory.CreateDesignator(func.Item1, func.Item2), assignVal);
                        break;

                    default:
                        throw new PDDLProblemLoaderException(PDDLErrorID.ExprInitCanHaveOnlyPredicates);
                }
            }

            if (requirements.Contains(DomainRequirements.ActionCosts) && !totalCostAssigned)
                throw new PDDLProblemLoaderException(PDDLErrorID.ActionCostsNotInitedToZero);
        }

        /// <summary>
        /// Processes the PDDL ':goal' block.
        /// </summary>
        /// <param name="str">Input string.</param>
        private static void ProcessGoal(string str)
        {
            ExpressionBuilder exprBuilder = new ExpressionBuilder(new Dictionary<string, Tuple<int, int>>(), 0, ExprType.Precondition);
            goalConditions = exprBuilder.BuildTree(str);
        }

        /// <summary>
        /// Expression token.
        /// </summary>
        private enum ExprToken
        {
            AND, OR, IMPLY, NOT, FORALL, EXISTS, WHEN, PREDICATE, EQUALS,
            NUM_REL_LT, NUM_REL_LTE, NUM_REL_GT, NUM_REL_GTE,
            ASSIGN, INCREASE, DECREASE
        }

        /// <summary>
        /// Gets the expression token from input string.
        /// </summary>
        /// <param name="strToken">Input string.</param>
        /// <returns>Expression token.</returns>
        private static ExprToken GetExpressionToken(string strToken)
        {
            switch (strToken)
            {
                case "and": return ExprToken.AND;
                case "or": return ExprToken.OR;
                case "imply": return ExprToken.IMPLY;
                case "not": return ExprToken.NOT;
                case "forall": return ExprToken.FORALL;
                case "exists": return ExprToken.EXISTS;
                case "when": return ExprToken.WHEN;
                case "<": return ExprToken.NUM_REL_LT;
                case "<=": return ExprToken.NUM_REL_LTE;
                case ">": return ExprToken.NUM_REL_GT;
                case ">=": return ExprToken.NUM_REL_GTE;
                case "=": return ExprToken.EQUALS;
                case "assign": return ExprToken.ASSIGN;
                case "increase": return ExprToken.INCREASE;
                case "decrease": return ExprToken.DECREASE;
            }

            return ExprToken.PREDICATE;
        }

        /// <summary>
        /// Expression type - precondition, effect, or simple-effect.
        /// </summary>
        private enum ExprType
        {
            Precondition,    // action preconditions + goal conditions
            Effect,          // action effects (SIMPLE_EFFECT + conditional effects "when <PRECONDITION> <SIMPLE_EFFECT>" + "forall <list> <EFFECT>")
            SimpleEffect,    // effects inside conditional effect - only pred, not(pred), 1-level and(...)
        }

        /// <summary>
        /// Auxiliary class for building PDDL expressions (for goal conditions, operator preconditions, ...).
        /// </summary>
        private class ExpressionBuilder
        {
            /// <summary>
            /// Mapping of variable names to their corresponding variable IDs and type IDs.
            /// </summary>
            private Dictionary<string, Tuple<int, int>> varNameToIDAndTypeMapping;

            /// <summary>
            /// Currently free variable ID to be used.
            /// </summary>
            private int freeVariableIdx;

            /// <summary>
            /// Type of expression that is being processed.
            /// </summary>
            private ExprType exprType;

            /// <summary>
            /// Constructs the expression builder.
            /// </summary>
            /// <param name="varNameToIDAndTypeMapping">Mapping of variable names to corresponding IDs and type IDs.</param>
            /// <param name="firstFreeVariableIdx">Currently free variable ID to be used.</param>
            /// <param name="exprType">Type of expression that is being processed.</param>
            public ExpressionBuilder(Dictionary<string, Tuple<int, int>> varNameToIDAndTypeMapping, int firstFreeVariableIdx, ExprType exprType)
            {
                this.varNameToIDAndTypeMapping = varNameToIDAndTypeMapping;
                this.freeVariableIdx = firstFreeVariableIdx;
                this.exprType = exprType;
            }

            /// <summary>
            /// Builds PDDL expression from the input string.
            /// </summary>
            /// <param name="expr">Expression in a form of string.</param>
            /// <returns>PDDL expression.</returns>
            public IPDDLLogicalExpression BuildTree(string expr)
            {
                return BuildTree(FetchNextBlock(expr));
            }

            /// <summary>
            /// Build PDDL expression from the fetched block.
            /// </summary>
            /// <param name="expr">Partially parsed expression in a fetched block.</param>
            /// <returns>PDDL expression.</returns>
            public IPDDLLogicalExpression BuildTree(FetchedBlock expr)
            {
                if (expr.blockHeader == null || (expr.blockHeader.Length == 0 && expr.blockBody.Length == 0))
                    return new EmptyExpr();
                else if (expr.rest.Trim().Length != 0 || (expr.blockHeader.Length == 0 && expr.blockBody.Length != 0))
                    throw new PDDLProblemLoaderException(PDDLErrorID.ExprCannotBeListOfExprs);

                ExprToken tokenType = GetExpressionToken(expr.blockHeader);
                switch (tokenType)
                {
                    case ExprToken.AND:
                    case ExprToken.OR:
                    case ExprToken.IMPLY:
                    case ExprToken.NOT:
                    case ExprToken.WHEN:

                        List<IPDDLLogicalExpression> subExpressions = new List<IPDDLLogicalExpression>();
                        string restStr = expr.blockBody;
                        int paramsCount = 0;

                        while (restStr.Length != 0)
                        {
                            ExprType useExprType = exprType;
                            if (tokenType == ExprToken.WHEN) // first expr of WHEN is precondition, second one is the result effect
                                useExprType = (paramsCount == 0) ? ExprType.Precondition : ExprType.SimpleEffect;

                            FetchedBlock currBlock = FetchNextBlock(restStr);
                            restStr = currBlock.rest;
                            currBlock.rest = "";
                            subExpressions.Add(new ExpressionBuilder(varNameToIDAndTypeMapping, freeVariableIdx, useExprType).BuildTree(currBlock));
                            ++paramsCount;
                        }

                        if (paramsCount == 0)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprLogicalOperatorArgsCount, expr.blockHeader);

                        if (tokenType == ExprToken.AND)
                        {
                            if (exprType == ExprType.Effect || exprType == ExprType.SimpleEffect)
                            {
                                foreach (var subExprr in subExpressions)
                                {
                                    if (subExprr is AndExpr)
                                        throw new PDDLProblemLoaderException(PDDLErrorID.ExprEffectMultiLevelAndOperator);
                                }
                            }

                            return new AndExpr(subExpressions.ToArray());
                        }
                        else if (tokenType == ExprToken.OR)
                        {
                            if (exprType == ExprType.Effect || exprType == ExprType.SimpleEffect)
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprEffectOrOperator);

                            if (!requirements.Contains(DomainRequirements.DisjunctivePrecond))
                                throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqDisjunctivePrecond);

                            return new OrExpr(subExpressions.ToArray());
                        }
                        else if (tokenType == ExprToken.IMPLY)
                        {
                            if (paramsCount != 2)
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprImplyOperatorArgsCount);

                            if (exprType == ExprType.Effect || exprType == ExprType.SimpleEffect)
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprEffectImplyOperator);

                            if (!requirements.Contains(DomainRequirements.DisjunctivePrecond))
                                throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqDisjunctivePrecond);

                            return new ImplyExpr(subExpressions[0], subExpressions[1]);
                        }
                        else if (tokenType == ExprToken.NOT)
                        {
                            if (paramsCount != 1)
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprNotOperatorArgsCount);

                            if (exprType == ExprType.Effect || exprType == ExprType.SimpleEffect)
                            {
                                // 'not' is allowed in effects as an atomic effect (requirement is not needed here)
                                if (!(subExpressions[0] is PredicateExpr))
                                    throw new PDDLProblemLoaderException(PDDLErrorID.ExprEffectNotOperator);
                            }
                            else if (exprType == ExprType.Precondition)
                            {
                                // 'not' can be used with equality operator even without specifying :negative-preconditions requirement
                                if (!requirements.Contains(DomainRequirements.NegativePrecond) && !(subExpressions[0] is EqualsExpr))
                                    throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqNegativePrecond);
                            }

                            return new NotExpr(subExpressions[0]);
                        }
                        else if (tokenType == ExprToken.WHEN)
                        {
                            if (paramsCount != 2)
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprWhenOperatorArgsCount);

                            if (exprType == ExprType.SimpleEffect)
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprEffectWhenOperatorInCondEff);

                            if (exprType == ExprType.Precondition)
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprPrecondWhenOperator);

                            if (!requirements.Contains(DomainRequirements.ConditionalEffects))
                                throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqConditionalEffects);

                            return new WhenExpr(subExpressions[0], subExpressions[1]);
                        }
                        break;

                    case ExprToken.FORALL:
                    case ExprToken.EXISTS:

                        if (tokenType == ExprToken.EXISTS)
                        {
                            if (exprType != ExprType.Precondition)
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprEffectExistsOperator);
                            else if (!requirements.Contains(DomainRequirements.ExistentialPrecond))
                                throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqExistentialPrecond);
                        }
                        else if (tokenType == ExprToken.FORALL)
                        {
                            if (exprType == ExprType.Precondition && !requirements.Contains(DomainRequirements.UniversalPrecond))
                                throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqUniversalPrecond);
                            else if (exprType == ExprType.Effect && !requirements.Contains(DomainRequirements.ConditionalEffects))
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprEffectForallOperatorReqMissing);
                            else if (exprType == ExprType.SimpleEffect)
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprEffectForallOperatorInCondEff);
                        }

                        FetchedBlock typedListStr = FetchNextBlock(expr.blockBody, true);
                        FetchedBlock subExprStr = FetchNextBlock(typedListStr.rest);

                        if (subExprStr.rest.Trim().Length != 0)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprQuantifiersArgsCount);

                        List<Tuple<string, int, int, string>> subExprVarList = new List<Tuple<string, int, int, string>>(); // varName:varID:typeID
                        ProcessTypedList(typedListStr.blockBody, subExprVarList, TokenType.Variable, freeVariableIdx);

                        Dictionary<int, int> varIDToTypeIDMapping = new Dictionary<int, int>();
                        for (int i = 0; i < subExprVarList.Count; ++i)
                        {
                            if (!CheckName(subExprVarList[i].Item1, true))
                                throw new PDDLProblemLoaderException(PDDLErrorID.InvalidNameForVariable, subExprVarList[i].Item1);

                            varIDToTypeIDMapping.Add(subExprVarList[i].Item2, subExprVarList[i].Item3); // paramID:typeID

                            if (varNameToIDAndTypeMapping.ContainsKey(subExprVarList[i].Item1))
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprVariableNameDuplicit, subExprVarList[i].Item1);
                            varNameToIDAndTypeMapping.Add(subExprVarList[i].Item1, Tuple.Create(subExprVarList[i].Item2, subExprVarList[i].Item3)); // varName:paramID:typeID
                        }

                        IPDDLLogicalExpression subExpr = new ExpressionBuilder(varNameToIDAndTypeMapping, freeVariableIdx, exprType).BuildTree(subExprStr);

                        for (int i = 0; i < subExprVarList.Count; ++i)
                        {
                            // we are out of the forall/exists context - remove these variables from the mapping
                            varNameToIDAndTypeMapping.Remove(subExprVarList[i].Item1); // varName
                        }
                        freeVariableIdx += subExprVarList.Count;

                        if (tokenType == ExprToken.FORALL)
                            return new ForallExpr(subExpr, varIDToTypeIDMapping);
                        else // if (tokenType == ExprToken.EXISTS)
                            return new ExistsExpr(subExpr, varIDToTypeIDMapping);

                    case ExprToken.PREDICATE:

                        var predParams = ProcessExprPredicateOrFunction(expr, PredFuncProcessingMode.PREDICATE, varNameToIDAndTypeMapping);
                        return new PredicateExpr(designatorFactory.CreateDesignator(predParams.Item1, predParams.Item2, predParams.Item3));

                    case ExprToken.EQUALS:
                    case ExprToken.ASSIGN:

                        bool isEquals = (tokenType == ExprToken.EQUALS);
                        bool isAssign = (tokenType == ExprToken.ASSIGN);

                        if (isEquals && exprType != ExprType.Precondition)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprEqualsInEffect);
                        else if (isAssign && exprType == ExprType.Precondition)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprAssignOpInPrecond);

                        List<IPDDLArithmeticExpression> numericExprList;
                        bool isOk = TryParseParamListAsArithmetic(expr.blockBody, out numericExprList);

                        if (requirements.Contains(DomainRequirements.ActionCosts) && numericExprList.Count > 0 && (numericExprList[0] is FunctionExpr)
                            && ((FunctionExpr)numericExprList[0]).function.GetPrefixID() == idManager.GetFunctionsMapping().GetFunctionID("total-cost"))
                            throw new PDDLProblemLoaderException((isEquals) ? PDDLErrorID.ActionCostsCannotBeInPrecond : PDDLErrorID.ActionCostsCanOnlyIncrease);

                        if (numericExprList.Count > 0 && !requirements.Contains(DomainRequirements.NumericFluents))
                            throw new PDDLProblemLoaderException((isEquals) ? PDDLErrorID.MissingReqNumFluentsForEqOp : PDDLErrorID.MissingReqNumFluentsForAssignOp);

                        if (isOk)
                        {
                            // process as numeric-wise equality/assignment

                            if (numericExprList.Count != 2)
                                throw new PDDLProblemLoaderException((isEquals) ? PDDLErrorID.ExprEqualsArgsCount : PDDLErrorID.ExprAssignOpArgsCount);

                            if (isAssign && !(numericExprList[0] is FunctionExpr))
                                throw new PDDLProblemLoaderException(PDDLErrorID.ExprAssignOpFirstArgFunc);

                            if (isEquals)
                                return new RelationalOperatorEqExpr(numericExprList[0], numericExprList[1]);
                            else // isAssign
                                return new NumAssignExpr(((FunctionExpr)numericExprList[0]).function, numericExprList[1]);
                        }
                        else if (!isOk && numericExprList.Count > 0)
                        {
                            // there are some numeric expressions, but something is wrong
                            throw new PDDLProblemLoaderException((isEquals) ? PDDLErrorID.ExprEqualsHasMixOfNumAndNonNumExprs : PDDLErrorID.ExprAssignOpHasMixOfNumAndNonNumExprs);
                        }

                        // process as standard object-wise equality

                        if (isEquals && !requirements.Contains(DomainRequirements.Equality))
                            throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqEquality);
                        else if (isAssign && !requirements.Contains(DomainRequirements.ObjectFluents))
                            throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqObjFluentsForAssignOp);

                        var argsList = ParseConstVarBlockMixedList(expr.blockBody);

                        if (argsList.Count != 2)
                            throw new PDDLProblemLoaderException((isEquals) ? PDDLErrorID.ExprEqualsArgsCount : PDDLErrorID.ExprAssignOpArgsCount);

                        List<int> paramIDs = new List<int>();
                        List<bool> paramIDsIsVar = new List<bool>();
                        List<IPDDLDesignator> functions = new List<IPDDLDesignator>();
                        int[] typeIDsForChecking = new int[2];

                        for (int j = 0; j < 2; ++j)
                        {
                            FetchedBlock wrappedParam = argsList[j];
                            if (wrappedParam.blockHeader == "") // constant or variable
                            {
                                if (j == 0 && isAssign)
                                    throw new PDDLProblemLoaderException(PDDLErrorID.ExprAssignOpFirstArgFunc);

                                string paramName = wrappedParam.blockBody;

                                if (paramName.Length > 0 && paramName[0] == '?') // param is variable
                                {
                                    if (!varNameToIDAndTypeMapping.ContainsKey(paramName))
                                        throw new PDDLProblemLoaderException((isEquals) ? PDDLErrorID.ExprPredicateUnknownVariable : PDDLErrorID.ExprFunctionUnknownVariable, expr.blockHeader, paramName);

                                    paramIDs.Add(varNameToIDAndTypeMapping[paramName].Item1);
                                    paramIDsIsVar.Add(true);
                                    typeIDsForChecking[j] = varNameToIDAndTypeMapping[paramName].Item2;
                                }
                                else // param is const
                                {
                                    int constID = idManager.GetConstantsMapping().GetConstID(paramName);
                                    if (constID == -1)
                                        throw new PDDLProblemLoaderException((isEquals) ? PDDLErrorID.ExprPredicateUnknownConstant : PDDLErrorID.ExprFunctionUnknownConstant, expr.blockHeader, paramName);

                                    paramIDs.Add(constID);
                                    paramIDsIsVar.Add(false);
                                    typeIDsForChecking[j] = idManager.GetConstantsMapping().GetTypeID(paramName);
                                }
                            }
                            else // param is an object function
                            {
                                var funcParams = ProcessExprPredicateOrFunction(wrappedParam, PredFuncProcessingMode.OBJECT_FUNCTION, varNameToIDAndTypeMapping);

                                int funcReturnType = idManager.GetFunctionsMapping().GetReturnType(wrappedParam.blockHeader);

                                functions.Add(designatorFactory.CreateDesignator(funcParams.Item1, funcParams.Item2, funcParams.Item3));
                                typeIDsForChecking[j] = funcReturnType;
                            }
                        }

                        if (typeIDsForChecking[0] != typeIDsForChecking[1])
                            throw new PDDLProblemLoaderException((isEquals) ? PDDLErrorID.ExprEqualsParamsTypeMismatch : PDDLErrorID.ExprAssignOpParamsTypeMismatch);

                        if (isEquals)
                        {
                            int[] paramIDArr = paramIDs.ToArray();
                            bool[] isVarArr = paramIDsIsVar.ToArray();

                            IPDDLDesignator equalPred = (paramIDArr.Length == 0) ? null : designatorFactory.CreateDesignator(-1, paramIDArr, isVarArr);

                            return new EqualsExpr(equalPred, functions.ToArray());
                        }
                        else // isAssign
                        {
                            if (functions.Count == 2)
                                return new ObjAssignExpr(functions[0], functions[1]);
                            else
                                return new ObjAssignExpr(functions[0], paramIDs[0], paramIDsIsVar[0]);
                        }

                    case ExprToken.NUM_REL_LT:
                    case ExprToken.NUM_REL_LTE:
                    case ExprToken.NUM_REL_GT:
                    case ExprToken.NUM_REL_GTE:

                        if (exprType != ExprType.Precondition)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprRelationalOpInEffect, expr.blockHeader);

                        List<IPDDLArithmeticExpression> numExprList;
                        bool isParamsOk = TryParseParamListAsArithmetic(expr.blockBody, out numExprList);

                        if (!isParamsOk)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprRelationalOpHasNonNumericArgs, expr.blockHeader);

                        if (!requirements.Contains(DomainRequirements.NumericFluents))
                            throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqNumFluentsForRelOp);

                        if (numExprList.Count != 2)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprRelationalOpArgsCount, expr.blockHeader);

                        if (tokenType == ExprToken.NUM_REL_LT)
                            return new RelationalOperatorLTExpr(numExprList[0], numExprList[1]);
                        else if (tokenType == ExprToken.NUM_REL_LTE)
                            return new RelationalOperatorLTEExpr(numExprList[0], numExprList[1]);
                        else if (tokenType == ExprToken.NUM_REL_GT)
                            return new RelationalOperatorGTExpr(numExprList[0], numExprList[1]);
                        else //if (tokenType == ExprToken.NUM_REL_GTE)
                            return new RelationalOperatorGTEExpr(numExprList[0], numExprList[1]);

                    case ExprToken.INCREASE:
                    case ExprToken.DECREASE:

                        if (exprType == ExprType.Precondition)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprIncrDecrOpInPrecond, expr.blockHeader);

                        List<IPDDLArithmeticExpression> assignExprList;
                        bool isParamsAllNumeric = TryParseParamListAsArithmetic(expr.blockBody, out assignExprList);

                        bool funcIsActionCost = (requirements.Contains(DomainRequirements.ActionCosts) && assignExprList.Count > 0 && (assignExprList[0] is FunctionExpr)
                            && ((FunctionExpr)assignExprList[0]).function.GetPrefixID() == idManager.GetFunctionsMapping().GetFunctionID("total-cost"));

                        if (funcIsActionCost && tokenType != ExprToken.INCREASE)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ActionCostsCanOnlyIncrease);

                        if (!funcIsActionCost && !requirements.Contains(DomainRequirements.NumericFluents))
                            throw new PDDLProblemLoaderException(PDDLErrorID.MissingReqNumFluentsForIncrDecrOp, expr.blockHeader);

                        if (!isParamsAllNumeric)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprIncrDecrOpHasNonNumericArgs, expr.blockHeader);

                        if (assignExprList.Count != 2)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprIncrDecrOpArgsCount, expr.blockHeader);

                        if (!(assignExprList[0] is FunctionExpr))
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprIncrDecrOpFirstArgFunc, expr.blockHeader);

                        if (tokenType == ExprToken.INCREASE)
                            return new IncreaseExpr(((FunctionExpr)assignExprList[0]).function, assignExprList[1]);
                        else // if (tokenType == ExprToken.DECREASE)
                            return new DecreaseExpr(((FunctionExpr)assignExprList[0]).function, assignExprList[1]);
                }

                throw new PDDLProblemLoaderException(PDDLErrorID.ExprInvalidToken, expr.blockHeader);
            }

            /// <summary>
            /// Processing mode for processExprPredicateOrFunction(...) method.
            /// </summary>
            public enum PredFuncProcessingMode
            {
                PREDICATE, OBJECT_FUNCTION, NUMERIC_FUNCTION, PREDICATE_INIT, FUNCTION_INIT
            }

            /// <summary>
            /// Processes a predicate or a function from the fetched block.
            /// </summary>
            /// <param name="expr">Partially parsed predicate or function.</param>
            /// <param name="processingMode">Processing mode.</param>
            /// <param name="varNameToIDAndTypeMapping">Mapping of variable names to variable IDs and type IDs.</param>
            /// <returns>Tuple of (designatorID, arrParameterIDs, arrIsParamVars).</returns>
            static public Tuple<int, int[], bool[]> ProcessExprPredicateOrFunction(FetchedBlock expr, PredFuncProcessingMode processingMode, Dictionary<string, Tuple<int, int>> varNameToIDAndTypeMapping)
            {
                bool isFunc = (processingMode != PredFuncProcessingMode.PREDICATE && processingMode != PredFuncProcessingMode.PREDICATE_INIT);
                bool isInit = (processingMode == PredFuncProcessingMode.PREDICATE_INIT || processingMode == PredFuncProcessingMode.FUNCTION_INIT);

                string[] argsList = expr.blockBody.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                string predFuncName = expr.blockHeader;
                int predFuncID = -1;

                if (!isFunc)
                    predFuncID = idManager.GetPredicatesMapping().GetPredicateID(predFuncName);
                else
                    predFuncID = idManager.GetFunctionsMapping().GetFunctionID(predFuncName);

                if (predFuncID == -1)
                    throw new PDDLProblemLoaderException((!isFunc) ? PDDLErrorID.ExprUndefinedPredicate : PDDLErrorID.ExprUndefinedFunction, predFuncName);

                if (!isFunc && argsList.Length != idManager.GetPredicatesMapping().GetNumberOfParameters(predFuncName))
                    throw new PDDLProblemLoaderException(PDDLErrorID.ExprPredicateArgsCount, predFuncName);
                else if (isFunc && argsList.Length != idManager.GetFunctionsMapping().GetNumberOfParameters(predFuncName))
                    throw new PDDLProblemLoaderException(PDDLErrorID.ExprFunctionArgsCount, predFuncName);

                int[] paramIDs = new int[argsList.Length];
                bool[] paramIDsIsVar = new bool[argsList.Length];

                for (int j = 0; j < argsList.Length; ++j)
                {
                    string paramName = argsList[j];
                    int typeID = -1;
                    int expectedTypeID = (!isFunc) ? idManager.GetPredicatesMapping().GetParameterTypeID(expr.blockHeader, j)
                        : idManager.GetFunctionsMapping().GetParameterTypeID(expr.blockHeader, j);

                    if (paramName.Length > 0 && paramName[0] == '?') // param is variable
                    {
                        if (isInit)
                            throw new PDDLProblemLoaderException(PDDLErrorID.ExprInitCannotHaveLiftedParams, paramName);

                        if (!varNameToIDAndTypeMapping.ContainsKey(paramName))
                            throw new PDDLProblemLoaderException((!isFunc) ? PDDLErrorID.ExprPredicateUnknownVariable : PDDLErrorID.ExprFunctionUnknownVariable, expr.blockHeader, paramName);

                        typeID = varNameToIDAndTypeMapping[paramName].Item2;

                        paramIDs[j] = varNameToIDAndTypeMapping[paramName].Item1;
                        paramIDsIsVar[j] = true;
                    }
                    else // param is const
                    {
                        int constID = idManager.GetConstantsMapping().GetConstID(paramName);
                        if (constID == -1)
                            throw new PDDLProblemLoaderException((!isFunc) ? PDDLErrorID.ExprPredicateUnknownConstant : PDDLErrorID.ExprFunctionUnknownConstant, expr.blockHeader, paramName);

                        typeID = idManager.GetConstantsMapping().GetTypeID(paramName);

                        paramIDs[j] = constID;
                        paramIDsIsVar[j] = false;
                    }

                    if (!idManager.GetTypesMapping().DoesTypeComply(expectedTypeID, typeID))
                        throw new PDDLProblemLoaderException((!isFunc) ? PDDLErrorID.ExprPredicateArgTypeMismatch : PDDLErrorID.ExprFunctionArgTypeMismatch, expr.blockHeader, paramName);
                }

                return Tuple.Create(predFuncID, paramIDs, paramIDsIsVar);
            }

            /// <summary>
            /// Processes a mixed list of constants, variables and complex expressions, e.g. ( ?a constA (+ 2 3) (func A B) ).
            /// </summary>
            /// <param name="strToProcess">Input list to be processed.</param>
            /// <returns>List of fetched blocks.</returns>
            public List<FetchedBlock> ParseConstVarBlockMixedList(string strToProcess)
            {
                List<FetchedBlock> retList = new List<FetchedBlock>();
                strToProcess = strToProcess.Trim();

                while (strToProcess.Length != 0)
                {
                    if (strToProcess[0] == '(') // param is a complex expression
                    {
                        FetchedBlock block = FetchNextBlock(strToProcess);
                        strToProcess = block.rest.Trim();
                        block.rest = "";
                        retList.Add(block);
                    }
                    else // param is a constant or variable
                    {
                        string primitiveToken;
                        int idxWhiteSpace = Array.FindIndex(strToProcess.ToCharArray(), x => char.IsWhiteSpace(x));
                        if (idxWhiteSpace != -1)
                        {
                            primitiveToken = strToProcess.Substring(0, idxWhiteSpace).Trim();
                            strToProcess = strToProcess.Substring(idxWhiteSpace).Trim();
                        }
                        else
                        {
                            primitiveToken = strToProcess.Trim();
                            strToProcess = "";
                        }

                        FetchedBlock wrapperBlock = new FetchedBlock();
                        wrapperBlock.blockHeader = "";
                        wrapperBlock.blockBody = primitiveToken;
                        wrapperBlock.rest = "";

                        retList.Add(wrapperBlock);
                    }
                }

                return retList;
            }

            /// <summary>
            /// Token types for arithmetic expressions.
            /// </summary>
            enum ArithmToken
            {
                PLUS, MINUS, MUL, DIV, NUM_CONST, FUNC
            }

            /// <summary>
            /// Get an arithmetic expression token frmo the input string.
            /// </summary>
            /// <param name="exprHeader">Input string.</param>
            /// <returns>Arithmetic expression token.</returns>
            private ArithmToken GetArithmToken(string exprHeader)
            {
                switch (exprHeader)
                {
                    case "+": return ArithmToken.PLUS;
                    case "-": return ArithmToken.MINUS;
                    case "*": return ArithmToken.MUL;
                    case "/": return ArithmToken.DIV;
                }

                return ArithmToken.FUNC;
            }

            /// <summary>
            /// Tries to parse the input string as a list of arithmetic expressions.
            /// </summary>
            /// <param name="strToProcess">Input string.</param>
            /// <param name="retNumExprList">Return list of arithmetic expressions.</param>
            /// <returns>True if the input string is a list of arithmetic expressions, false otherwise.</returns>
            public bool TryParseParamListAsArithmetic(string strToProcess, out List<IPDDLArithmeticExpression> retNumExprList)
            {
                retNumExprList = new List<IPDDLArithmeticExpression>();
                bool someError = false;

                List<FetchedBlock> wrappedExprList = ParseConstVarBlockMixedList(strToProcess);
                foreach (var wrappedExpr in wrappedExprList)
                {
                    if (wrappedExpr.blockHeader != "") // param is possibly a complex arithmetic expression (+,-,*,/) or a function
                    {
                        ArithmToken arithmToken = GetArithmToken(wrappedExpr.blockHeader);

                        if (arithmToken == ArithmToken.FUNC)
                        {
                            var funcExpr = ProcessExprPredicateOrFunction(wrappedExpr, PredFuncProcessingMode.NUMERIC_FUNCTION, varNameToIDAndTypeMapping);

                            bool isActionCost = requirements.Contains(DomainRequirements.ActionCosts) && funcExpr.Item1 == idManager.GetFunctionsMapping().GetFunctionID("total-cost");
                            if (isActionCost && exprType == ExprType.Precondition)
                                throw new PDDLProblemLoaderException(PDDLErrorID.ActionCostsCannotBeInPrecond);

                            if (idManager.GetFunctionsMapping().GetReturnType(wrappedExpr.blockHeader) != idManager.GetTypesMapping().GetNumericTypeID())
                            {
                                someError = true; // non-numeric function
                                continue;
                            }

                            retNumExprList.Add(new FunctionExpr(designatorFactory.CreateDesignator(funcExpr.Item1, funcExpr.Item2, funcExpr.Item3)));
                            continue;
                        }

                        List<IPDDLArithmeticExpression> subExprList;
                        bool subExprOk = TryParseParamListAsArithmetic(wrappedExpr.blockBody, out subExprList);
                        if (subExprOk)
                        {
                            switch (arithmToken)
                            {
                                case ArithmToken.PLUS:
                                case ArithmToken.MUL:
                                    if (subExprList.Count < 2)
                                        throw new PDDLProblemLoaderException(PDDLErrorID.ExprArithmeticMultiOpArgsCount, wrappedExpr.blockHeader);
                                    break;
                                case ArithmToken.DIV:
                                    if (subExprList.Count != 2)
                                        throw new PDDLProblemLoaderException(PDDLErrorID.ExprArithmeticBinaryOpArgsCount, wrappedExpr.blockHeader);
                                    break;
                                case ArithmToken.MINUS:
                                    if (subExprList.Count != 2 && subExprList.Count != 1)
                                        throw new PDDLProblemLoaderException(PDDLErrorID.ExprArithmeticMinusOpArgsCount);
                                    break;
                            }

                            IPDDLArithmeticExpression newExpr = null;
                            switch (arithmToken)
                            {
                                case ArithmToken.PLUS:
                                    newExpr = new PlusExpr(subExprList.ToArray());
                                    break;
                                case ArithmToken.MINUS:
                                    if (subExprList.Count == 1)
                                        newExpr = new UnaryMinus(subExprList[0]);
                                    else
                                        newExpr = new MinusExpr(subExprList[0], subExprList[1]);
                                    break;
                                case ArithmToken.MUL:
                                    newExpr = new MulExpr(subExprList.ToArray());
                                    break;
                                case ArithmToken.DIV:
                                    newExpr = new DivExpr(subExprList[0], subExprList[1]);
                                    break;
                            }

                            retNumExprList.Add(newExpr);
                        }
                        else
                            someError = true;
                    }
                    else // param is possibly a numeric constant
                    {
                        string exprConstant = wrappedExpr.blockBody;

                        int exprConstantNum;
                        if (Int32.TryParse(exprConstant, out exprConstantNum))
                            retNumExprList.Add(new ConstExpr(exprConstantNum));
                        else
                            someError = true; // non-numeric constant or variable
                    }

                }

                return !someError;
            }
        }
    }
}
