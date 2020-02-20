﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;


namespace Microsoft.Quantum.QsCompiler.Transformations.MonomorphizationValidationTransformation
{
    public class MonomorphizationValidationTransformation : QsSyntaxTreeTransformation<MonomorphizationValidationTransformation.TransformationState>
    {
        public static void Apply(QsCompilation compilation)
        {
            var filter = new MonomorphizationValidationTransformation();

            foreach (var ns in compilation.Namespaces)
            {
                filter.Namespaces.Transform(ns);
            }
        }

        public class TransformationState { }

        public MonomorphizationValidationTransformation() : base(new TransformationState()) 
        { 
            this.Namespaces = new NamespaceTransformation(this);
            this.Expressions = new ExpressionTransformation(this);
            this.Types = new TypeTransformation(this);
        }

        private class NamespaceTransformation : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(QsSyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

            public override ResolvedSignature onSignature(ResolvedSignature s)
            {
                if (s.TypeParameters.Any())
                {
                    throw new Exception("Signatures cannot contains type parameters");
                }

                return base.onSignature(s);
            }
        }

        private class ExpressionTransformation : Core.ExpressionTransformation<TransformationState>
        {
            public ExpressionTransformation(QsSyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

            public override ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> onTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> typeParams)
            {
                if (typeParams.Any())
                {
                    throw new Exception("Type Parameter Resolutions must be empty");
                }

                return typeParams;
            }
        }

        private class TypeTransformation : TypeTransformation<TransformationState>
        {
            public TypeTransformation(QsSyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

            public override QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> onTypeParameter(QsTypeParameter tp)
            {
                throw new Exception("Type Parameter types must be resolved");
            }
        }
    }
}