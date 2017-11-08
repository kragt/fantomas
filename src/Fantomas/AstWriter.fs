module internal Fantomas.AstWriter

open System
open System.IO
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices
open System.Text



let writeAstString (ast: ParsedInput) =
    let sb = new StringBuilder()

    let indent i =
        String.replicate i "·   "
    
    let append (s: string) =
        sb.Append s |> ignore

    let appendLine (s: string) =
        sb.AppendLine s |> ignore

    let appendWithIndent i (s: string) =
        sb.Append (indent i) |> ignore
        sb.Append s |> ignore

    let appendLineWithIndent i s =
        sb.Append (indent i) |> ignore
        sb.AppendLine s |> ignore

    let lidToString (lid: LongIdent) =
        lid
        |> List.map (fun x -> x.idText)
        |> String.concat "."

    let lidwdToString lidwd =
        let (LongIdentWithDots(lid, _)) = lidwd
        lidToString lid

    let visitBinding binding =
        let (Binding(_, _, _, _, _, _, _, pat, _, _, _, _)) = binding
        match pat with
        | SynPat.Const _ -> appendLine (sprintf "Const")
        | SynPat.Wild _ -> appendLine (sprintf "Wild")
        | SynPat.Named _ -> appendLine (sprintf "Named")
        | SynPat.Typed _ -> appendLine (sprintf "Typed")
        | SynPat.Attrib _ -> appendLine (sprintf "Attrib")
        | SynPat.Or _ -> appendLine (sprintf "Or")
        | SynPat.Ands _ -> appendLine (sprintf "Ands")
        | SynPat.LongIdent (lidwd, lid, _, _, _, _) -> 
            match lid with
            | Some lid -> appendLine (sprintf "LongIdent: %s %s" (lidwdToString lidwd) lid.idText)
            | None -> appendLine (sprintf "LongIdent: %s" (lidwdToString lidwd))
        | SynPat.Tuple _ -> appendLine (sprintf "Tuple")
        | SynPat.StructTuple _ -> appendLine (sprintf "StructTuple")
        | SynPat.Paren _ -> appendLine (sprintf "Paren")
        | SynPat.ArrayOrList _ -> appendLine (sprintf "ArrayOrList")
        | SynPat.Record _ -> appendLine (sprintf "Record")
        | SynPat.Null _ -> appendLine (sprintf "Null")
        | SynPat.OptionalVal _ -> appendLine (sprintf "OptionalVal")
        | SynPat.IsInst _ -> appendLine (sprintf "IsInst")
        | SynPat.QuoteExpr _ -> appendLine (sprintf "QuoteExpr")
        | SynPat.DeprecatedCharRange _ -> appendLine (sprintf "DeprecatedCharRange")
        | SynPat.InstanceMember _ -> appendLine (sprintf "InstanceMember")
        | SynPat.FromParseError _ -> appendLine (sprintf "FromParseError")

    let visitMemberDefns ilevel defns =
        for d in defns do
            match d with
            | SynMemberDefn.Open (lid, _) -> appendLineWithIndent ilevel (sprintf "Open: %s" (lidToString lid))
            | SynMemberDefn.Member (defn, _) -> 
                appendWithIndent ilevel (sprintf "Member: ")
                visitBinding defn
            | SynMemberDefn.ImplicitCtor _ -> appendLineWithIndent ilevel (sprintf "Ctor: ")
            | SynMemberDefn.ImplicitInherit _ -> appendLineWithIndent ilevel (sprintf "ImplicitInherit: ")
            | SynMemberDefn.LetBindings _ -> appendLineWithIndent ilevel (sprintf "LetBindings: ")
            | SynMemberDefn.AbstractSlot _ -> appendLineWithIndent ilevel (sprintf "AbstractSlot: ")
            | SynMemberDefn.Interface _ -> appendLineWithIndent ilevel (sprintf "Interface: ")
            | SynMemberDefn.Inherit _ -> appendLineWithIndent ilevel (sprintf "Inherit: ")
            | SynMemberDefn.ValField (sf, _) -> 
                let (Field(_, _, id, _, _, _, _, _)) = sf
                match id with
                | Some id -> appendLineWithIndent ilevel (sprintf "ValField: %s" id.idText)
                | None -> appendLineWithIndent ilevel (sprintf "ValField: Anon")
            | SynMemberDefn.NestedType _ -> appendLineWithIndent ilevel (sprintf "NestedType: ")
            | SynMemberDefn.AutoProperty _ -> appendLineWithIndent ilevel (sprintf "AutoProperty: ")

    let rec visitTypeDefns ilevel defns =
        for d in defns do
            let (TypeDefn(sci, repr, defns, _)) = d
            let (ComponentInfo(_, _, _, lid, _, _, _, _)) = sci
            appendWithIndent ilevel (sprintf "Type: %s " (lidToString lid))
            match repr with
            | SynTypeDefnRepr.ObjectModel (kind, defns, _) ->
                appendLine (sprintf "ObjectModel %A" kind)
                visitMemberDefns (ilevel + 1) defns
            | SynTypeDefnRepr.Simple _ -> appendLine "Simple"
            | SynTypeDefnRepr.Exception _ -> appendLine "Exception"
            //visitTypeDefnRepr (ilevel + 1) repr
            visitMemberDefns (ilevel + 1) defns

    let visitDeclarations ilevel decls =
        for d in decls do
            match d with
            | SynModuleDecl.ModuleAbbrev _ -> appendLineWithIndent ilevel "ModuleAbbrev"
            | SynModuleDecl.NestedModule _ -> appendLineWithIndent ilevel "NestedModule"
            | SynModuleDecl.Let _ -> appendLineWithIndent ilevel "Let"
            | SynModuleDecl.DoExpr _ -> appendLineWithIndent ilevel "DoExpr"
            | SynModuleDecl.Types (defns, _) -> visitTypeDefns ilevel defns
            | SynModuleDecl.Exception _ -> appendLineWithIndent ilevel "Exception"
            | SynModuleDecl.Open (lidwd, _) -> appendLineWithIndent ilevel (sprintf "Open: %s" (lidwdToString lidwd))
            | SynModuleDecl.Attributes _ -> appendLineWithIndent ilevel "Attributes"
            | SynModuleDecl.HashDirective _ -> appendLineWithIndent ilevel "HashDirective"
            | SynModuleDecl.NamespaceFragment _ -> appendLineWithIndent ilevel "NamespaceFragment"

    let visitModulesOrNamespaces ilevel mns =
        for mn in mns do
            let (SynModuleOrNamespace(lid, _isRec, _isMod, decls, _xml, _attrs, _, _m)) = mn
            sb.AppendLine (sprintf "Namespace or Module: %s" (lidToString lid)) |> ignore
            visitDeclarations (ilevel + 1) decls

    match ast with
    | ParsedInput.ImplFile impl -> 
        let (ParsedImplFileInput(_fn, _script, _name, _, _, mns, _)) = impl
        visitModulesOrNamespaces 0 mns
    | ParsedInput.SigFile _ -> sb.AppendLine "Signature file" |> ignore
    
    sb.ToString()

let writeFile (ast: ParsedInput) (fileName: string) =
    File.WriteAllText(fileName, writeAstString ast)