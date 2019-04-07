﻿//  Copyright 2019 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using NtApiDotNet.Ndr;
using System;
using System.CodeDom;

namespace NtApiDotNet.Win32.RpcClient
{
    internal enum RpcPointerType
    {
        None = 0,
        Reference,
        Unique,
        Full
    }

    internal struct RpcMarshalArgument
    {
        public CodeExpression Expression;
        public CodeTypeReference CodeType;
    }

    internal sealed class RpcTypeDescriptor
    {
        public CodeTypeReference CodeType { get; }
        public Type BuiltinType { get; }
        public NdrBaseTypeReference NdrType { get; }
        public RpcMarshalArgument[] AdditionalArgs { get; }
        public bool Pointer => PointerType != RpcPointerType.None;
        public RpcPointerType PointerType { get; }
        public bool ValueType { get; }
        public bool Constructed { get; }
        public string UnmarshalMethod { get; }
        public bool UnmarshalGeneric { get; }
        public string MarshalMethod { get; }

        public RpcTypeDescriptor(CodeTypeReference code_type, bool value_type, string unmarshal_method, 
            bool unmarshal_generic, string marshal_method, NdrBaseTypeReference ndr_type, params RpcMarshalArgument[] additional_args)
        {
            CodeType = code_type;
            UnmarshalMethod = unmarshal_method;
            MarshalMethod = marshal_method;
            UnmarshalGeneric = unmarshal_generic;
            NdrType = ndr_type;
            AdditionalArgs = additional_args;
            ValueType = value_type;
        }

        public RpcTypeDescriptor(Type code_type, string unmarshal_method, bool unmarshal_generic, 
            string marshal_method, NdrBaseTypeReference ndr_type, params RpcMarshalArgument[] additional_args)
            : this(new CodeTypeReference(code_type), code_type.IsValueType || typeof(NtObject).IsAssignableFrom(code_type), 
                  unmarshal_method, unmarshal_generic, marshal_method, ndr_type, additional_args)
        {
            BuiltinType = code_type;
        }

        public RpcTypeDescriptor(string name, bool value_type, string unmarshal_method, bool unmarshal_generic, 
            string marshal_method, NdrBaseTypeReference ndr_type, params RpcMarshalArgument[] additional_args)
            : this(new CodeTypeReference(name), value_type, unmarshal_method, unmarshal_generic, marshal_method, ndr_type, additional_args)
        {
            Constructed = true;
        }

        public RpcTypeDescriptor(RpcTypeDescriptor original_desc, RpcPointerType pointer_type)
            : this(original_desc.CodeType, false, original_desc.UnmarshalMethod, original_desc.UnmarshalGeneric,
            original_desc.MarshalMethod, original_desc.NdrType, original_desc.AdditionalArgs)
        {
            PointerType = pointer_type;
            Constructed = original_desc.Constructed;
        }

        public CodeMethodReferenceExpression GetMarshalMethod(CodeExpression target)
        {
            return new CodeMethodReferenceExpression(target, MarshalMethod);
        }

        public CodeMethodReferenceExpression GetUnmarshalMethod(CodeExpression target)
        {
            if (UnmarshalGeneric)
            {
                return new CodeMethodReferenceExpression(target, UnmarshalMethod, CodeType);
            }
            return new CodeMethodReferenceExpression(target, UnmarshalMethod);
        }

        public CodeTypeReference GetStructureType()
        {
            if (Pointer)
            {
                CodeTypeReference ret = new CodeTypeReference(typeof(NdrEmbeddedPointer<>));
                ret.TypeArguments.Add(CodeType);
                return ret;
            }
            return CodeType;
        }

        public CodeTypeReference GetParameterType()
        {
            if (Pointer && ValueType)
            {
                CodeTypeReference ret = new CodeTypeReference(typeof(Nullable<>));
                ret.TypeArguments.Add(CodeType);
                return ret;
            }
            return CodeType;
        }
    }
}
