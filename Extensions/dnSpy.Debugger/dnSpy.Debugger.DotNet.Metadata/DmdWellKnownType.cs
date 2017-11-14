﻿// Copied from Roslyn's SpecialType and WellKnownType enums

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Well known types
	/// </summary>
	public enum DmdWellKnownType {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		// Roslyn: SpecialType
		System_Object,
		System_Enum,
		System_MulticastDelegate,
		System_Delegate,
		System_ValueType,
		System_Void,
		System_Boolean,
		System_Char,
		System_SByte,
		System_Byte,
		System_Int16,
		System_UInt16,
		System_Int32,
		System_UInt32,
		System_Int64,
		System_UInt64,
		System_Decimal,
		System_Single,
		System_Double,
		System_String,
		System_IntPtr,
		System_UIntPtr,
		System_Array,
		System_Collections_IEnumerable,
		System_Collections_Generic_IEnumerable_T,
		System_Collections_Generic_IList_T,
		System_Collections_Generic_ICollection_T,
		System_Collections_IEnumerator,
		System_Collections_Generic_IEnumerator_T,
		System_Collections_Generic_IReadOnlyList_T,
		System_Collections_Generic_IReadOnlyCollection_T,
		System_Nullable_T,
		System_DateTime,
		System_Runtime_CompilerServices_IsVolatile,
		System_IDisposable,
		System_TypedReference,
		System_ArgIterator,
		System_RuntimeArgumentHandle,
		System_RuntimeFieldHandle,
		System_RuntimeMethodHandle,
		System_RuntimeTypeHandle,
		System_IAsyncResult,
		System_AsyncCallback,

		// Roslyn: WellKnownType except dupes
		System_Math,
		System_Attribute,
		System_CLSCompliantAttribute,
		System_Convert,
		System_Exception,
		System_FlagsAttribute,
		System_FormattableString,
		System_Guid,
		System_IFormattable,
		System_MarshalByRefObject,
		System_Type,
		System_Reflection_AssemblyKeyFileAttribute,
		System_Reflection_AssemblyKeyNameAttribute,
		System_Reflection_MethodInfo,
		System_Reflection_ConstructorInfo,
		System_Reflection_MethodBase,
		System_Reflection_FieldInfo,
		System_Reflection_MemberInfo,
		System_Reflection_Missing,
		System_Runtime_CompilerServices_FormattableStringFactory,
		System_Runtime_CompilerServices_RuntimeHelpers,
		System_Runtime_ExceptionServices_ExceptionDispatchInfo,
		System_Runtime_InteropServices_StructLayoutAttribute,
		System_Runtime_InteropServices_UnknownWrapper,
		System_Runtime_InteropServices_DispatchWrapper,
		System_Runtime_InteropServices_CallingConvention,
		System_Runtime_InteropServices_ClassInterfaceAttribute,
		System_Runtime_InteropServices_ClassInterfaceType,
		System_Runtime_InteropServices_CoClassAttribute,
		System_Runtime_InteropServices_ComAwareEventInfo,
		System_Runtime_InteropServices_ComEventInterfaceAttribute,
		System_Runtime_InteropServices_ComInterfaceType,
		System_Runtime_InteropServices_ComSourceInterfacesAttribute,
		System_Runtime_InteropServices_ComVisibleAttribute,
		System_Runtime_InteropServices_DispIdAttribute,
		System_Runtime_InteropServices_GuidAttribute,
		System_Runtime_InteropServices_InterfaceTypeAttribute,
		System_Runtime_InteropServices_Marshal,
		System_Runtime_InteropServices_TypeIdentifierAttribute,
		System_Runtime_InteropServices_BestFitMappingAttribute,
		System_Runtime_InteropServices_DefaultParameterValueAttribute,
		System_Runtime_InteropServices_LCIDConversionAttribute,
		System_Runtime_InteropServices_UnmanagedFunctionPointerAttribute,
		System_Activator,
		System_Threading_Tasks_Task,
		System_Threading_Tasks_Task_T,
		System_Threading_Interlocked,
		System_Threading_Monitor,
		System_Threading_Thread,
		Microsoft_CSharp_RuntimeBinder_Binder,
		Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo,
		Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfoFlags,
		Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags,
		Microsoft_VisualBasic_CallType,
		Microsoft_VisualBasic_Embedded,
		Microsoft_VisualBasic_CompilerServices_Conversions,
		Microsoft_VisualBasic_CompilerServices_Operators,
		Microsoft_VisualBasic_CompilerServices_NewLateBinding,
		Microsoft_VisualBasic_CompilerServices_EmbeddedOperators,
		Microsoft_VisualBasic_CompilerServices_StandardModuleAttribute,
		Microsoft_VisualBasic_CompilerServices_Utils,
		Microsoft_VisualBasic_CompilerServices_LikeOperator,
		Microsoft_VisualBasic_CompilerServices_ProjectData,
		Microsoft_VisualBasic_CompilerServices_ObjectFlowControl,
		Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl,
		Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag,
		Microsoft_VisualBasic_CompilerServices_StringType,
		Microsoft_VisualBasic_CompilerServices_IncompleteInitialization,
		Microsoft_VisualBasic_CompilerServices_Versioned,
		Microsoft_VisualBasic_CompareMethod,
		Microsoft_VisualBasic_Strings,
		Microsoft_VisualBasic_ErrObject,
		Microsoft_VisualBasic_FileSystem,
		Microsoft_VisualBasic_ApplicationServices_ApplicationBase,
		Microsoft_VisualBasic_ApplicationServices_WindowsFormsApplicationBase,
		Microsoft_VisualBasic_Information,
		Microsoft_VisualBasic_Interaction,

		System_Func_T,
		System_Func_T2,
		System_Func_T3,
		System_Func_T4,
		System_Func_T5,
		System_Func_T6,
		System_Func_T7,
		System_Func_T8,
		System_Func_T9,
		System_Func_T10,
		System_Func_T11,
		System_Func_T12,
		System_Func_T13,
		System_Func_T14,
		System_Func_T15,
		System_Func_T16,
		System_Func_T17,

		System_Action,
		System_Action_T,
		System_Action_T2,
		System_Action_T3,
		System_Action_T4,
		System_Action_T5,
		System_Action_T6,
		System_Action_T7,
		System_Action_T8,
		System_Action_T9,
		System_Action_T10,
		System_Action_T11,
		System_Action_T12,
		System_Action_T13,
		System_Action_T14,
		System_Action_T15,
		System_Action_T16,

		System_AttributeUsageAttribute,
		System_ParamArrayAttribute,
		System_NonSerializedAttribute,
		System_STAThreadAttribute,
		System_Reflection_DefaultMemberAttribute,
		System_Runtime_CompilerServices_DateTimeConstantAttribute,
		System_Runtime_CompilerServices_DecimalConstantAttribute,
		System_Runtime_CompilerServices_IUnknownConstantAttribute,
		System_Runtime_CompilerServices_IDispatchConstantAttribute,
		System_Runtime_CompilerServices_ExtensionAttribute,
		System_Runtime_CompilerServices_INotifyCompletion,
		System_Runtime_CompilerServices_InternalsVisibleToAttribute,
		System_Runtime_CompilerServices_CompilerGeneratedAttribute,
		System_Runtime_CompilerServices_AccessedThroughPropertyAttribute,
		System_Runtime_CompilerServices_CompilationRelaxationsAttribute,
		System_Runtime_CompilerServices_RuntimeCompatibilityAttribute,
		System_Runtime_CompilerServices_UnsafeValueTypeAttribute,
		System_Runtime_CompilerServices_FixedBufferAttribute,
		System_Runtime_CompilerServices_DynamicAttribute,
		System_Runtime_CompilerServices_CallSiteBinder,
		System_Runtime_CompilerServices_CallSite,
		System_Runtime_CompilerServices_CallSite_T,

		System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken,
		System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T,
		System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal,

		Windows_Foundation_IAsyncAction,
		Windows_Foundation_IAsyncActionWithProgress_T,
		Windows_Foundation_IAsyncOperation_T,
		Windows_Foundation_IAsyncOperationWithProgress_T2,

		System_Diagnostics_Debugger,
		System_Diagnostics_DebuggerDisplayAttribute,
		System_Diagnostics_DebuggerNonUserCodeAttribute,
		System_Diagnostics_DebuggerHiddenAttribute,
		System_Diagnostics_DebuggerBrowsableAttribute,
		System_Diagnostics_DebuggerStepThroughAttribute,
		System_Diagnostics_DebuggerBrowsableState,
		System_Diagnostics_DebuggableAttribute,
		System_Diagnostics_DebuggableAttribute__DebuggingModes,

		System_ComponentModel_DesignerSerializationVisibilityAttribute,

		System_IEquatable_T,

		System_Collections_IList,
		System_Collections_ICollection,
		System_Collections_Generic_EqualityComparer_T,
		System_Collections_Generic_List_T,
		System_Collections_Generic_IDictionary_KV,
		System_Collections_Generic_IReadOnlyDictionary_KV,
		System_Collections_ObjectModel_Collection_T,
		System_Collections_ObjectModel_ReadOnlyCollection_T,
		System_Collections_Specialized_INotifyCollectionChanged,
		System_ComponentModel_INotifyPropertyChanged,
		System_ComponentModel_EditorBrowsableAttribute,
		System_ComponentModel_EditorBrowsableState,

		System_Linq_Enumerable,
		System_Linq_Expressions_Expression,
		System_Linq_Expressions_Expression_T,
		System_Linq_Expressions_ParameterExpression,
		System_Linq_Expressions_ElementInit,
		System_Linq_Expressions_MemberBinding,
		System_Linq_Expressions_ExpressionType,
		System_Linq_IQueryable,
		System_Linq_IQueryable_T,

		System_Xml_Linq_Extensions,
		System_Xml_Linq_XAttribute,
		System_Xml_Linq_XCData,
		System_Xml_Linq_XComment,
		System_Xml_Linq_XContainer,
		System_Xml_Linq_XDeclaration,
		System_Xml_Linq_XDocument,
		System_Xml_Linq_XElement,
		System_Xml_Linq_XName,
		System_Xml_Linq_XNamespace,
		System_Xml_Linq_XObject,
		System_Xml_Linq_XProcessingInstruction,

		System_Security_UnverifiableCodeAttribute,
		System_Security_Permissions_SecurityAction,
		System_Security_Permissions_SecurityAttribute,
		System_Security_Permissions_SecurityPermissionAttribute,

		System_NotSupportedException,

		System_Runtime_CompilerServices_ICriticalNotifyCompletion,
		System_Runtime_CompilerServices_IAsyncStateMachine,
		System_Runtime_CompilerServices_AsyncVoidMethodBuilder,
		System_Runtime_CompilerServices_AsyncTaskMethodBuilder,
		System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T,
		System_Runtime_CompilerServices_AsyncStateMachineAttribute,
		System_Runtime_CompilerServices_IteratorStateMachineAttribute,

		System_Windows_Forms_Form,
		System_Windows_Forms_Application,

		System_Environment,

		System_Runtime_GCLatencyMode,
		System_IFormatProvider,

		System_ValueTuple_T1,
		System_ValueTuple_T2,
		System_ValueTuple_T3,
		System_ValueTuple_T4,
		System_ValueTuple_T5,
		System_ValueTuple_T6,

		System_ValueTuple_T7,
		System_ValueTuple_TRest,

		System_Runtime_CompilerServices_TupleElementNamesAttribute,

		System_Runtime_CompilerServices_ReferenceAssemblyAttribute,

		System_ContextBoundObject,

		System_Runtime_CompilerServices_TypeForwardedToAttribute,
		System_Runtime_InteropServices_ComImportAttribute,
		System_Runtime_InteropServices_DllImportAttribute,
		System_Runtime_InteropServices_FieldOffsetAttribute,
		System_Runtime_InteropServices_InAttribute,
		System_Runtime_InteropServices_MarshalAsAttribute,
		System_Runtime_InteropServices_OptionalAttribute,
		System_Runtime_InteropServices_OutAttribute,
		System_Runtime_InteropServices_PreserveSigAttribute,
		System_SerializableAttribute,
		System_Runtime_InteropServices_CharSet,
		System_Reflection_Assembly,
		System_RuntimeMethodHandleInternal,
		System_ByReference_T,
		System_Runtime_InteropServices_UnmanagedType,
		System_Runtime_InteropServices_VarEnum,
		System___ComObject,
		System_Runtime_InteropServices_WindowsRuntime_RuntimeClass,
		System_DBNull,
		System_Security_Permissions_PermissionSetAttribute,
		System_Diagnostics_Debugger_CrossThreadDependencyNotification,
		System_Diagnostics_DebuggerTypeProxyAttribute,
		System_Collections_Generic_KeyValuePair_T2,
		System_Linq_SystemCore_EnumerableDebugView,
		System_Linq_SystemCore_EnumerableDebugView_T,
		System_Linq_SystemCore_EnumerableDebugViewEmptyException,
		System_Text_Encoding,
		System_Runtime_CompilerServices_IsReadOnlyAttribute,
		System_Runtime_CompilerServices_IsByRefLikeAttribute,
		System_ObsoleteAttribute,
		System_Span_T,

		// When adding more types, update DmdWellKnownTypeUtils

		None = -1,
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
