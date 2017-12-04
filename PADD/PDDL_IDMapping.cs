using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADD
{
    /// <summary>
    /// Manager for the ID mappings used in the PDDL planning problem. Handles identifiers of predicates, functions, types and constants.
    /// </summary>
    public class PDDLIdentifierMappingsManager
    {
        /// <summary>
        /// Predicates ID mapping. Stores the data about original predicate names and their mapped IDs, number of parameters and their types.
        /// </summary>
        private PDDLPredicatesMapping predicatesMapping = new PDDLPredicatesMapping();

        /// <summary>
        /// Functions ID mapping. Stores the data about original function names and their mapped IDs, return types, number of parameters and their types.
        /// </summary>
        private PDDLFunctionsMapping functionsMapping = new PDDLFunctionsMapping();

        /// <summary>
        /// Constants ID mapping. Stores the data about original constant names and their mapped IDs and type IDs.
        /// </summary>
        private PDDLConstantsMapping constsMapping = new PDDLConstantsMapping();

        /// <summary>
        /// Types ID mapping. Stores the data about original type names and their mapped IDs, and the data about type hierarchy.
        /// </summary>
        private PDDLTypesMapping typesMapping = new PDDLTypesMapping();

        /// <summary>
        /// Gets the predicates ID mapping. Stores the data about predicate names and their mapped IDs, number of parameters and their types.
        /// </summary>
        /// <returns>Predicates ID mapping.</returns>
        public PDDLPredicatesMapping GetPredicatesMapping()
        {
            return predicatesMapping;
        }

        /// <summary>
        /// Gets the functions ID mapping. Stores the data about function names and their mapped IDs, return types, number of parameters and their types.
        /// </summary>
        /// <returns>Functions ID mapping.</returns>
        public PDDLFunctionsMapping GetFunctionsMapping()
        {
            return functionsMapping;
        }

        /// <summary>
        /// Gets the constants ID mapping. Stores the data about constant names and their mapped IDs and type IDs.
        /// </summary>
        /// <returns>Constants ID mapping.</returns>
        public PDDLConstantsMapping GetConstantsMapping()
        {
            return constsMapping;
        }

        /// <summary>
        /// Gets the types ID mapping. Stores the data about type names and their mapped IDs, and the data about type hierarchy.
        /// </summary>
        /// <returns>Types ID mapping.</returns>
        public PDDLTypesMapping GetTypesMapping()
        {
            return typesMapping;
        }

        /// <summary>
        /// Gets a list of constant IDs of the constants that are of specified type or its subtypes.
        /// </summary>
        /// <param name="typeID">Requested type ID.</param>
        /// <returns>List of constant IDs of the specified type or its subtypes.</returns>
        public List<int> GetConstantsIDForType(int typeID)
        {
            List<int> constList = new List<int>();

            constList.AddRange(constsMapping.GetConstantsIDs(typeID));

            foreach (var childTypeID in typesMapping.GetChildrenTypeIDs(typeID))
                constList.AddRange(GetConstantsIDForType(childTypeID));

            return constList;
        }
    }

    /// <summary>
    /// Predicates ID mapping. Stores the data about original predicate names and their mapped IDs, number of parameters and their types.
    /// </summary>
    public class PDDLPredicatesMapping
    {
        /// <summary>
        /// Mapping of the original predicate name to corresponding ID and its parameters type IDs.
        /// </summary>
        private Dictionary<string, Tuple<int,int[]>> predicateStringToID = new Dictionary<string, Tuple<int,int[]>>();

        /// <summary>
        /// Mapping of predicate ID back to its original name.
        /// </summary>
        private Dictionary<int, string> predicateIDToString = new Dictionary<int, string>();

        /// <summary>
        /// Predicate ID to be used for the next registration.
        /// </summary>
        private int currFreeID = 0;

        /// <summary>
        /// Registers the specified predicate and its parameters.
        /// </summary>
        /// <param name="predName">Original name of the predicate.</param>
        /// <param name="parametersTypeID">List of corresponding type IDs of the predicate parameters.</param>
        /// <returns>False if the predicate was registered before, true otherwise.</returns>
        public bool SetPredicate(string predName, int[] parametersTypeID)
        {
            if (predicateStringToID.ContainsKey(predName))
                return false;

            predicateStringToID.Add(predName, Tuple.Create(currFreeID, parametersTypeID));
            predicateIDToString.Add(currFreeID, predName);
            ++currFreeID;
            return true;
        }

        /// <summary>
        /// Gets the predicate ID from its name.
        /// </summary>
        /// <param name="predName">Original predicate name.</param>
        /// <returns>Mapped predicate ID.</returns>
        public int GetPredicateID(string predName)
        {
            if (!predicateStringToID.ContainsKey(predName))
                return -1;

            return predicateStringToID[predName].Item1;
        }

        /// <summary>
        /// Gets the type ID of the specified parameter of the predicate.
        /// </summary>
        /// <param name="predName">Predicate name.</param>
        /// <param name="paramIdx">Index of the predicate parameter.</param>
        /// <returns>Type ID of the predicate parameter.</returns>
        public int GetParameterTypeID(string predName, int paramIdx)
        {
            if (!predicateStringToID.ContainsKey(predName))
                return -1;

            return predicateStringToID[predName].Item2[paramIdx];
        }

        /// <summary>
        /// Gets the number of parameters of the specified predicate.
        /// </summary>
        /// <param name="predName">Predicate name.</param>
        /// <returns>Number of predicate parameters.</returns>
        public int GetNumberOfParameters(string predName)
        {
            return predicateStringToID[predName].Item2.Length;
        }

        /// <summary>
        /// Gets the original predicate name from its mapped ID.
        /// </summary>
        /// <param name="predID">Mapped predicate ID.</param>
        /// <returns>Original predicate name.</returns>
        public string GetStringForPredicateID(int predID)
        {
            return predicateIDToString[predID];
        }
    }

    /// <summary>
    /// Functions ID mapping. Stores the data about original function names and their mapped IDs, return types, number of parameters and their types.
    /// </summary>
    public class PDDLFunctionsMapping
    {
        /// <summary>
        /// Mapping of the original function name to corresponding ID, its parameters type IDs, and its return type.
        /// </summary>
        private Dictionary<string, Tuple<int, int[], int>> functionStringToID = new Dictionary<string, Tuple<int, int[], int>>();

        /// <summary>
        /// Mapping of function ID back to its original name.
        /// </summary>
        private Dictionary<int, string> functionIDToString = new Dictionary<int, string>();

        /// <summary>
        /// Function ID to be used for the next registration.
        /// </summary>
        private int currFreeID = 0;

        /// <summary>
        /// Registers the specified function, its parameters and a return type.
        /// </summary>
        /// <param name="funcName">Original name of the function.</param>
        /// <param name="parametersTypeID">List of corresponding type IDs of the function parameters.</param>
        /// <param name="returnTypeID">Function return type ID.</param>
        /// <returns>False if the function was registered before, true otherwise.</returns>
        public bool SetFunction(string funcName, int[] parametersTypeID, int returnTypeID)
        {
            if (functionStringToID.ContainsKey(funcName))
                return false;

            functionStringToID.Add(funcName, Tuple.Create(currFreeID, parametersTypeID, returnTypeID));
            functionIDToString.Add(currFreeID, funcName);
            ++currFreeID;
            return true;
        }

        /// <summary>
        /// Gets the function ID from its name.
        /// </summary>
        /// <param name="funcName">Original function name.</param>
        /// <returns>Mapped function ID.</returns>
        public int GetFunctionID(string funcName)
        {
            if (!functionStringToID.ContainsKey(funcName))
                return -1;

            return functionStringToID[funcName].Item1;
        }

        /// <summary>
        /// Gets the type ID of the specified parameter of the function.
        /// </summary>
        /// <param name="funcName">Function name.</param>
        /// <param name="paramIdx">Index of the function parameter.</param>
        /// <returns>Type ID of the function parameter.</returns>
        public int GetParameterTypeID(string funcName, int paramIdx)
        {
            if (!functionStringToID.ContainsKey(funcName))
                return -1;

            return functionStringToID[funcName].Item2[paramIdx];
        }

        /// <summary>
        /// Gets the number of parameters of the specified function.
        /// </summary>
        /// <param name="funcName">Function name.</param>
        /// <returns>Number of function parameters.</returns>
        public int GetNumberOfParameters(string funcName)
        {
            return functionStringToID[funcName].Item2.Length;
        }

        /// <summary>
        /// Gets the return type ID of the function.
        /// </summary>
        /// <param name="funcName">Function name.</param>
        /// <returns>Return type ID of the function.</returns>
        public int GetReturnType(string funcName)
        {
            if (!functionStringToID.ContainsKey(funcName))
                return -1;
            return functionStringToID[funcName].Item3;
        }

        /// <summary>
        /// Gets the original function name from its mapped ID.
        /// </summary>
        /// <param name="funcID">Mapped function ID.</param>
        /// <returns>Original function name.</returns>
        public string GetStringForFunctionID(int funcID)
        {
            return functionIDToString[funcID];
        }
    }

    /// <summary>
    /// Constants ID mapping. Stores the data about original constant names and their mapped IDs and type IDs.
    /// </summary>
    public class PDDLConstantsMapping
    {
        /// <summary>
        /// Mapping of the original constant name to corresponding ID and type ID.
        /// </summary>
        private Dictionary<string, Tuple<int,int>> constantStringToID = new Dictionary<string, Tuple<int,int>>();

        /// <summary>
        /// Mapping of constant ID back to its original name.
        /// </summary>
        private Dictionary<int, string> constantsIDToString = new Dictionary<int, string>();

        /// <summary>
        /// Lists of constant IDs for the specified types.
        /// </summary>
        private Dictionary<int, List<int>> typesToConstListMapping = new Dictionary<int, List<int>>();

        /// <summary>
        /// Constant ID to be used for the next registration.
        /// </summary>
        private int currFreeID = 0;

        /// <summary>
        /// Registers the specified constant with its corresponding type.
        /// </summary>
        /// <param name="constName">Original constant name.</param>
        /// <param name="typeID">Type ID of the constant.</param>
        /// <returns>False if the function was registered before, true otherwise.</returns>
        public bool SetConst(string constName, int typeID)
        {
            if (constantStringToID.ContainsKey(constName))
                return false;

            constantStringToID.Add(constName, Tuple.Create(currFreeID, typeID));
            constantsIDToString.Add(currFreeID, constName);

            if (typesToConstListMapping.ContainsKey(typeID))
            {
                typesToConstListMapping[typeID].Add(currFreeID);
            }
            else
            {
                List<int> newList = new List<int>();
                newList.Add(currFreeID);
                typesToConstListMapping.Add(typeID, newList);
            }

            currFreeID++;
            return true;
        }

        /// <summary>
        /// Gets the constant ID from its name.
        /// </summary>
        /// <param name="constName">Original constant name.</param>
        /// <returns>Mapped constant ID.</returns>
        public int GetConstID(string constName)
        {
            if (!constantStringToID.ContainsKey(constName))
                return -1;

            return constantStringToID[constName].Item1;
        }

        /// <summary>
        /// Gets the type ID of the specified constant.
        /// </summary>
        /// <param name="constName">Constant nsme.</param>
        /// <returns>Type ID of the constant.</returns>
        public int GetTypeID(string constName)
        {
            if (!constantStringToID.ContainsKey(constName))
                return -1;

            return constantStringToID[constName].Item2;
        }

        /// <summary>
        /// Gets all of the constants of the specified type.
        /// </summary>
        /// <param name="typeID">Type ID.</param>
        /// <returns>List of all constants for the type.</returns>
        public List<int> GetConstantsIDs(int typeID)
        {
            if (typesToConstListMapping.ContainsKey(typeID))
                return typesToConstListMapping[typeID];
            return new List<int>();
        }

        /// <summary>
        /// Gets the current free constant ID to be used.
        /// </summary>
        /// <returns>Free constant ID available.</returns>
        public int GetFirstFreeConstID()
        {
            return currFreeID;
        }

        /// <summary>
        /// Gets the original constant name from its mapped ID.
        /// </summary>
        /// <param name="constID">Mapped constant ID.</param>
        /// <returns>Original constant name.</returns>
        public string GetStringForConstID(int constID)
        {
            if (!constantsIDToString.ContainsKey(constID))
                return "?" + constID.ToString();
            return constantsIDToString[constID];
        }
    }

    /// <summary>
    /// Types ID mapping. Stores the data about original type names and their mapped IDs, and the data about type hierarchy.
    /// </summary>
    public class PDDLTypesMapping
    {
        /// <summary>
        /// Special type ID for the PDDL numeric type ('number').
        /// </summary>
        private const int NUMERIC_FLUENT_TYPE_ID = -2;

        /// <summary>
        /// Mapping of the original type name to corresponding ID.
        /// </summary>
        private Dictionary<string, int> typeStringToID = new Dictionary<string, int>();

        /// <summary>
        /// Type hierarchy - list of children types (their IDs) for the given type ID.
        /// </summary>
        private Dictionary<int, List<int>> typeChildrenList = new Dictionary<int, List<int>>();

        /// <summary>
        /// Type ID to be used for the next registration.
        /// </summary>
        private int currFreeID = 0;

        /// <summary>
        /// Constructs an empty type mapping with a PDDL super-type 'object' already specified.
        /// </summary>
        public PDDLTypesMapping()
        {
            typeStringToID.Add("object", currFreeID);
            typeChildrenList.Add(currFreeID, new List<int>());
            ++currFreeID;
        }

        /// <summary>
        /// Registers the specified type with its parent type. Registers the parent type as well, if it wasn't specified before.
        /// </summary>
        /// <param name="typeName">Original name of the type.</param>
        /// <param name="parentType">Original name of the parent type.</param>
        /// <returns>False if the type was registered before, true otherwise.</returns>
        public bool SetType(string typeName, string parentType = "object")
        {
            if (typeStringToID.ContainsKey(typeName))
                return false;

            int parentTypeID = GetTypeID(parentType);
            if (parentTypeID == -1)
            {
                SetType(parentType);
                parentTypeID = GetTypeID(parentType);
            }

            typeStringToID.Add(typeName, currFreeID);
            typeChildrenList.Add(currFreeID, new List<int>());

            typeChildrenList[parentTypeID].Add(currFreeID);

            currFreeID++;
            return true;
        }

        /// <summary>
        /// Gets the type ID from its name.
        /// </summary>
        /// <param name="typeName">Original type name.</param>
        /// <returns>Mapped type ID.</returns>
        public int GetTypeID(string typeName)
        {
            if (!typeStringToID.ContainsKey(typeName))
                return -1;

            return typeStringToID[typeName];
        }

        /// <summary>
        /// Gets the type ID from its name. Accounts also special numeric type (which is outside type hierarchy).
        /// </summary>
        /// <param name="typeName">Original type name.</param>
        /// <returns>Mapped type ID.</returns>
        public int GetTypeIDForFunction(string typeName)
        {
            if (typeName == "number")
                return NUMERIC_FLUENT_TYPE_ID;

            return GetTypeID(typeName);
        }

        /// <summary>
        /// Gets the special numeric type ID (built-in 'number' type in PDDL).
        /// </summary>
        /// <returns>Numeric type ID.</returns>
        public int GetNumericTypeID()
        {
            return NUMERIC_FLUENT_TYPE_ID;
        }

        /// <summary>
        /// Gets a list of children types (descendants) of the specified type in the type hierarchy.
        /// </summary>
        /// <param name="typeID">Requested type ID.</param>
        /// <returns>List of descendant types in the type hierarchy.</returns>
        public List<int> GetChildrenTypeIDs(int typeID)
        {
            if (!typeChildrenList.ContainsKey(typeID))
                return null;
            return typeChildrenList[typeID];
        }

        /// <summary>
        /// Checks whether the reference type is a super-type (or the same as) of the specified type.
        /// </summary>
        /// <param name="referenceType">Reference type.</param>
        /// <param name="currType">Specified type.</param>
        /// <returns>True if the reference type is a super-type (or the same as) of the specified type.</returns>
        public bool DoesTypeComply(int referenceType, int currType)
        {
            if (referenceType == currType)
                return true;

            return GetChildrenTypeIDs(referenceType).Contains(currType);
        }

    }
}
