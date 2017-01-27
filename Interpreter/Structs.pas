﻿
struct Code {
	int[] ops,
	int[][] args,
	string[] stringArgs,
}

// Dictionaries in Crayon can only have 3 types of keys: integers, strings, and objects
// Dictionaries can hold any of these, but only hold one type at a time
struct DictImpl {
	Dictionary<int, Value> keyIntLookup,
	Dictionary<int, Value> valueIntLookup,
	Dictionary<string, Value> keyStringLookup,
	Dictionary<string, Value> valueStringLookup,
	int size,
	int keyType
}

struct ProgramData {
	Dictionary<int, ExecutionContext> executionContexts,
	int lastExecutionContextId,
	int[] ops,
	int[][] args,
	string[] stringArgs,
	string[] identifiers,
	List<string> identifiersBuilder,
	Dictionary<string, int> invIdentifiers,
	Value[] literalTable,
	List<Value> literalTableBuilder,
	List<Token>[] tokenData,
	int userCodeStart,
	string[] sourceCode,
	List<string> sourceCodeBuilder,
	Dictionary<int, int>[] integerSwitchLookups,
	List<Dictionary<int, int>> integerSwitchLookupsBuilder,
	Dictionary<string, int>[] stringSwitchLookups,
	List<Dictionary<string, int>> stringSwitchLookupsBuilder,
	int instanceCounter,
	bool initializationComplete,

	ClassInfo[] classTable,
	FunctionInfo[] functionTable,
	List<int> classStaticInitializationStack,
	int[] globalNameIdToPrimitiveMethodName,
	Value[] funcArgs,

	int lengthId,
	int[] primitiveMethodFunctionIdFallbackLookup,
	ResourceDB resourceDatabase,
	string projectId,
	int[][] esfData,
	MagicNumbers magicNumbers,
	int[] valueStackDepthByPc,
}

struct ExecutionContext {
	int id,
	StackFrame stackTop,
	int currentValueStackSize,
	int valueStackCapacity,
	Value[] valueStack,
	Value[] localsStack,
	int[] localsStackSet,
	int localsStackSetToken,
	int executionCounter,
	bool activeExceptionHandled,
	Value activeException,
}

struct StackFrame {
	int pc,
	int localsStackSetToken, // if localsStackSet[i + offset] has this value, then that means localsStack[i + offset] is valid.
	int localsStackOffset, // offset local ID by this to get the value in localsStack
	int localsStackOffsetEnd, // ending offset of the locals stack
	StackFrame previous,
	bool returnValueUsed,
	Value objectContext,
	int valueStackPopSize, // size of the value stack when this frame is popped.
	int markClassAsInitialized,
	int depth, // stack depth for infinite recursion detection
	int postFinallyBehavior, // value defined in PostFinallyBehavior enum
	Value returnValueTempStorage, // return value when you return but still need to run a finally block
}

struct InterpreterResult {
	int status, // InterpreterResultStatus enum
	string errorMessage,
}

struct Token {
	int lineIndex,
	int colIndex,
	int fileId
}

struct Value {
	int type,
	object internalValue
}

struct SystemMethod {
	Value context,
	int id
}

/*
	When a class is about to be referenced, static initialization state is checked.
	If it is 0, then check to see if the base class chain has any 0's and initialize the last class
	up the chain. Class intializations work like function invocations where the return PC points to the
	exact same spot where the intialization was triggered, so that the code continues to run normally
	despite the interrupt of the static constructor.

	When a class is initialized, a new Value[] is allocated with length of .memberCount.
	Then a loop runs through all member ID/indexes and checks the fieldInitializationCommand.
	If it's a literal, it copies the value directly from the fieldInitializationLiteral in the class metadata here.
	If it's a LIST or DICT, then it creates an empty instance of those.
	LIST or DICT is only used if the field is initialized to an _empty_ List or Dictionary. Otherwise NULL is used.
	If it's a SYSTEM_NULL the value of null (not a Value, but an actual native null) is applied to that slot. This
	indicates that the member is actually referencing a method. This is lazily populated at runtime if a function
	reference without invocation is ever made.

	All other fields are populated by the constructor by the default field value code which is injected into the
	constructor's byte code between the base constructor invocation and the body of the constructor.
*/
struct ClassInfo {
	int id,
	int nameId,
	int baseClassId, // or -1 if no base class
	int staticInitializationState, // 0 - not initialized, 1 - in progress, 2 - done
	Value[] staticFields,
	int staticConstructorFunctionId,
	int constructorFunctionId,

	int memberCount, // total number of fields and methods, flattened.
	// The following lists contain data on all flattened fields and methods indexed by member ID.
	// These values are replicated in children classes where the child class' members appear at the end of the list.
	int[] functionIds, // function ID or -1
	int[] fieldInitializationCommand, // 0 - USE_LITERAL, 1 - LIST, 2 - DICT, 3 - SYSTEM NULL (reserved for method)
	Value[] fieldInitializationLiteral,

	// TODO: It might actually be good to create two of these, one that's global ID to all member ID's, and another
	// for global ID to assignable member ID's. This would eliminate a couple of CPU cycles and also partially pave
	// the road for private/protected/public modifiers.
	Dictionary<int, int> globalIdToMemberId,

	string fullyQualifiedName,
}

struct FunctionInfo {
	int id,
	int nameId,
	int pc,
	int minArgs,
	int maxArgs,
	int type, // 0 - function, 1 - method, 2 - static method, 3 - constructor, 4 - static constructor
	int associatedClassId,
	int localsSize,
	int[] pcOffsetsForOptionalArgs,
	string name,
}

// Used as the internalValue of a Value instance for Class definition types.
struct ClassValue {
	bool isInterface,
	int classId, // or interface ID (when that is eventually implemented)
}

struct ObjectInstance {
	int classId,
	int objectId,
	Value[] members,

	// system libraries can attach arbitrary values to objects, which can be operated on more efficiently than values, and
	// are inherently inaccessible by user code.
	object[] nativeData,
}

struct FunctionPointer {
	int type, // Enum value: FunctionPointerType
	Value context,
	int classId,
	int functionId,
}

struct PlatformRelayObject {
	int type,
	int iarg1,
	int iarg2,
	int iarg3,
	double farg1,
	string sarg1,
}

struct HttpRequest {
	int statusCode,
	string status,
	Dictionary<string, string[]> headers,
	string body,
}

struct ResourceInfo {
	string userPath,
	string internalPath,
	bool isText,
	string type, // e.g. SND, IMG, IMGSH, etc. Don't convert this to an enum. Want this to be extensible.
	string manifestParam, // e.g. the image sheet ID, multiple values here must be encoded if extended.
}

struct ResourceDB {
	Dictionary<string, string[]> filesPerDirectory,
	Dictionary<string, ResourceInfo> fileInfo,
}

struct MagicNumbers {
	int coreExceptionClassId,
	int coreGenerateExceptionFunctionId,
}
