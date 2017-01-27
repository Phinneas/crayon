﻿string getTypeFromId(int id) {
	switch (id) {
		case Types.NULL: return "null";
		case Types.BOOLEAN: return "boolean";
		case Types.INTEGER: return "integer";
		case Types.FLOAT: return "float";
		case Types.STRING: return "string";
		case Types.LIST: return "list";
		case Types.DICTIONARY: return "dictionary";
		case Types.INSTANCE: return "instance"; // TODO: make this more specific
		case Types.FUNCTION: return "function";
	}
	return null;
}

bool isClassASubclassOf(int subClassId, int parentClassId) {
	if (subClassId == parentClassId) return true;
	ProgramData p = Core.ProgramData;
	Array<ClassInfo> classTable = p.classTable;
	int classIdWalker = subClassId;
	while (classIdWalker != -1) {
		if (classIdWalker == parentClassId) return true;
		ClassInfo classInfo = classTable[classIdWalker];
		classIdWalker = classInfo.baseClassId;
	}
	return false;
}
