﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;


namespace Microsoft.Quantum.QsCompiler.Transformations.Monomorphization.Validation
{
    public class ValidateMonomorphization : SyntaxTreeTransformation<ValidateMonomorphization.TransformationState>
    {
        public static void Apply(QsCompilation compilation)
        {
            var intrinsicCallableSet = compilation.Namespaces.GlobalCallableResolutions()
                .Where(kvp => kvp.Value.Specializations.Any(spec => spec.Implementation.IsIntrinsic))
                .Select(kvp => kvp.Key)
                .ToImmutableHashSet();

            var filter = new ValidateMonomorphization(intrinsicCallableSet);

            foreach (var ns in compilation.Namespaces)
            {
                filter.Namespaces.OnNamespace(ns);
            }
        }

        public class TransformationState
        {
            public readonly ImmutableHashSet<QsQualifiedName> IntrinsicCallableSet;

            public TransformationState(ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
            {
                this.IntrinsicCallableSet = intrinsicCallableSet;
            }
        }

        internal ValidateMonomorphization(ImmutableHashSet<QsQualifiedName> intrinsicCallableSet) : base(new TransformationState(intrinsicCallableSet))
        {
            this.Namespaces = new NamespaceTransformation(this);
            this.Expressions = new ExpressionTransformation(this);
            this.Types = new TypeTransformation(this);
        }

        private class NamespaceTransformation : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

            public override QsCallable OnCallableDeclaration(QsCallable c)
            {
                // Don't validate intrinsics
                if (!SharedState.IntrinsicCallableSet.Contains(c.FullName))
                {
                    return base.OnCallableDeclaration(c);
                }
                else
                {
                    return c;
                }
            }

            public override ResolvedSignature OnSignature(ResolvedSignature s)
            {
                if (s.TypeParameters.Any())
                {
                    throw new Exception("Signatures cannot contains type parameters");
                }

                return base.OnSignature(s);
            }
        }

        private class ExpressionTransformation : ExpressionTransformation<TransformationState>
        {
            public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

            public override ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> OnTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> typeParams)
            {
                // Type resolutions for intrinsics are allowed
                if (typeParams.Any(kvp => !SharedState.IntrinsicCallableSet.Contains(kvp.Key.Item1)))
                {
                    throw new Exception("Type Parameter Resolutions must be empty");
                }

                return typeParams;
            }
        }

        private class TypeTransformation : TypeTransformation<TransformationState>
        {
            public TypeTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

            public override QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> OnTypeParameter(QsTypeParameter tp)
            {
                throw new Exception("Type Parameter types must be resolved");
            }
        }
    }
}
