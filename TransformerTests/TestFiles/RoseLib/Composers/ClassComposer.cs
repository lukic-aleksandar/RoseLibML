﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoseLib.Model;
using RoseLib.Traversal.Navigators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoseLib.Exceptions;
using RoseLib.Traversal;
using RoseLib.Enums;
using RoseLib.Guards;

namespace RoseLib.Composers
{
    public partial class ClassComposer: CSRTypeComposer
    {
        internal ClassComposer(IStatefulVisitor visitor, bool pivotOnParent = false) : base(visitor, pivotOnParent)
        {
        }

        #region Transition methods
        public static new bool CanProcessCurrentSelection(IStatefulVisitor statefulVisitor, bool pivotOnParent)
        {
            if(!pivotOnParent) 
            {
                return GenericCanProcessCurrentSelectionCheck(statefulVisitor, typeof(ClassDeclarationSyntax), SupportedScope.IMMEDIATE_OR_PARENT);
            }
            else
            {
                return GenericCanProcessCurrentSelectionParentCheck(statefulVisitor, typeof(ClassDeclarationSyntax));
            }
        }

        protected override void PrepareStateAndSetStatePivot(bool pivotOnParent)
        {
            if (!pivotOnParent)
            {
                GenericPrepareStateAndSetStatePivot(typeof(ClassDeclarationSyntax), SupportedScope.IMMEDIATE_OR_PARENT);
            }
            else
            {
                GenericPrepareStateAndSetParentAsStatePivot(typeof(ClassDeclarationSyntax));
            }
        }
        #endregion

        #region Addition methods
        public override ClassComposer AddField(FieldProps options)
        {
            return (base.AddFieldToNodeOfType<ClassDeclarationSyntax>(options) as ClassComposer)!;
        }

        public override ClassComposer AddProperty(PropertyProps options)
        {
            return (base.AddPropertyToType<ClassDeclarationSyntax>(options) as ClassComposer)!;
        }
        public override ClassComposer AddMethod(MethodProps options)
        {
            return (base.AddMethodToType<ClassDeclarationSyntax>(options) as ClassComposer)!;
        }

        public override ClassComposer SetAttributes(List<AttributeProps> modelAttributeList)
        {
            base.SetAttributes(modelAttributeList);

            return this;
        }
        #endregion

        #region Class change methods
        public ClassComposer Rename(string newName)
        {
            CompositionGuard.ImmediateNodeIs(Visitor.CurrentNode, typeof(ClassDeclarationSyntax));

            var identifier = SyntaxFactory.Identifier(newName);
            var renamedClass = (Visitor.CurrentNode as ClassDeclarationSyntax)!.WithIdentifier(identifier);
            var withAdjustedConstructors = RenameConstuctors(renamedClass, identifier) as ClassDeclarationSyntax;
            Visitor.ReplaceNodeAndAdjustState(Visitor.CurrentNode!, withAdjustedConstructors!);

            return this;
        }
        private SyntaxNode RenameConstuctors(SyntaxNode @class, SyntaxToken identifier)
        {
            // TODO: Rely on selector methods to find all the constructors.
            var constructorCount = @class.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Count();
            var newRoot = @class;

            for (var current = 0; current < constructorCount; current++)
            {
                var constructors = newRoot.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
                var ctor = constructors.ElementAt(current);

                var newCtor = ctor.WithIdentifier(identifier);
                newRoot = newRoot.ReplaceNode(ctor, newCtor);
            }

            return newRoot;
        }

        public ClassComposer SetBaseTypes(List<string>? baseTypes)
        {
            CompositionGuard.ImmediateNodeIs(Visitor.CurrentNode, typeof(ClassDeclarationSyntax));
            var @class = (Visitor.CurrentNode as ClassDeclarationSyntax)!;

            ClassDeclarationSyntax? alteredClass;
            if(baseTypes == null || baseTypes.Count() == 0)
            {
                alteredClass = @class.WithBaseList(null);
            }
            else
            {
                List<BaseTypeSyntax> parsedBaseTypes = new List<BaseTypeSyntax>();
                foreach(var baseType in baseTypes)
                {
                    var type = SyntaxFactory.ParseTypeName(baseType);
                    var parsedbaseType = SyntaxFactory.SimpleBaseType(type);
                    parsedBaseTypes.Add(parsedbaseType);
                }
                var syntaxList = SyntaxFactory.SeparatedList(parsedBaseTypes);
                var baseTypeList = SyntaxFactory.BaseList(syntaxList);
                alteredClass = @class.WithBaseList(baseTypeList);
            }

            Visitor.ReplaceNodeAndAdjustState(Visitor.CurrentNode!, alteredClass);

            return this;
        }

        public ClassComposer SetAccessModifier(AccessModifiers newType)
        {
            CompositionGuard.ImmediateNodeIs(Visitor.CurrentNode, typeof(ClassDeclarationSyntax));

            var @class = (Visitor.CurrentNode as ClassDeclarationSyntax)!;
            SyntaxTokenList modifiers = @class.Modifiers;
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                var m = modifiers.ElementAt(i);
                switch (m.Kind())
                {
                    case SyntaxKind.InternalKeyword:
                    case SyntaxKind.PublicKeyword:
                        modifiers = modifiers.RemoveAt(i);
                        break;
                }
            }

            switch (newType)
            {
                case AccessModifiers.PUBLIC:
                    modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                    break;
                case AccessModifiers.INTERNAL:
                    modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                    break;
                case AccessModifiers.NONE:
                    break;
                case AccessModifiers.PRIVATE:
                case AccessModifiers.PROTECTED:
                case AccessModifiers.PRIVATE_PROTECTED:
                case AccessModifiers.PROTECTED_INTERNAL:
                    throw new NotSupportedException($"Setting {newType} as an access modifier of a class not supported");
            }

            SyntaxNode withSetModifiers = @class.WithModifiers(modifiers);
            Visitor.ReplaceNodeAndAdjustState(Visitor.CurrentNode!, withSetModifiers);

            return this;
        }

        public ClassComposer MakeStatic()
        {
            CompositionGuard.ImmediateNodeIs(Visitor.CurrentNode, typeof(ClassDeclarationSyntax));

            var @class = (Visitor.CurrentNode as ClassDeclarationSyntax)!;

            SyntaxTokenList modifiers = @class.Modifiers;

            if (modifiers.Where(m => m.IsKind(SyntaxKind.StaticKeyword)).Any())
            {
                return this;
            }

            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            SyntaxNode madeStatic = @class.WithModifiers(modifiers);
            Visitor.ReplaceNodeAndAdjustState(Visitor.CurrentNode!, madeStatic);

            return this;
        }

        public ClassComposer MakeNonStatic()
        {
            CompositionGuard.ImmediateNodeIs(Visitor.CurrentNode, typeof(ClassDeclarationSyntax));

            var @class = (Visitor.CurrentNode as ClassDeclarationSyntax)!;
            SyntaxTokenList modifiers = @class.Modifiers;
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                var m = modifiers.ElementAt(i);
                if (m.IsKind(SyntaxKind.StaticKeyword))
                {
                    modifiers = modifiers.RemoveAt(i);
                    break;
                }
            }

            SyntaxNode madeNonStatic = @class.WithModifiers(modifiers);
            Visitor.ReplaceNodeAndAdjustState(Visitor.CurrentNode!, madeNonStatic);

            return this;
        }

        public ClassComposer MakePartial()
        {
            CompositionGuard.ImmediateNodeIs(Visitor.CurrentNode, typeof(ClassDeclarationSyntax));

            var @class = (Visitor.CurrentNode as ClassDeclarationSyntax)!;

            SyntaxTokenList modifiers = @class.Modifiers;

            if (modifiers.Where(m => m.IsKind(SyntaxKind.PartialKeyword)).Any())
            {
                return this;
            }

            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
            SyntaxNode madeStatic = @class.WithModifiers(modifiers);
            Visitor.ReplaceNodeAndAdjustState(Visitor.CurrentNode!, madeStatic);

            return this;
        }

        public ClassComposer MakeNonPartial()
        {
            CompositionGuard.ImmediateNodeIs(Visitor.CurrentNode, typeof(ClassDeclarationSyntax));

            var @class = (Visitor.CurrentNode as ClassDeclarationSyntax)!;
            SyntaxTokenList modifiers = @class.Modifiers;
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                var m = modifiers.ElementAt(i);
                if (m.IsKind(SyntaxKind.PartialKeyword))
                {
                    modifiers = modifiers.RemoveAt(i);
                    break;
                }
            }

            SyntaxNode madeNonStatic = @class.WithModifiers(modifiers);
            Visitor.ReplaceNodeAndAdjustState(Visitor.CurrentNode!, madeNonStatic);

            return this;
        }

        #endregion

        public ClassComposer Delete()
        {
            base.DeleteForParentNodeOfType<ClassDeclarationSyntax>();
            return this;
        }

    }
}
