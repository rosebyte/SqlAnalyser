﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using NUnit.Framework;
using SqlAnalyser.Internal;
using SqlAnalyser.Internal.Identifiers;
using SqlAnalyser.Internal.Visitors;

namespace SqlAnalyser.Tests.Internal.Visitors
{
    [TestFixture]
    public class ReferenceScannerTests
    {
	    private static List<IdentifierInfo> GetReferences(string sql, string schema = null, string database = null, 
		    string server = null)
	    {
		    var batches = SqlParser.Parse(sql, SqlVersion.Sql100, out var errors);
			
		    var sut = new ReferenceVisitor();
		    var result = new List<IdentifierInfo>();

		    foreach (var sqlBatch in batches)
		    {
			    (var references, var doers) = sut.GetReferences(sqlBatch, schema, database, server);
				    
			    result.AddRange(references); 
		    }
		    
		    return result;
	    }
	    
	    [Test]
	    public void ShouldFindTableReferenceInSelect()
	    {
		    const string sql = "SELECT * FROM myTable";
		    var reference = new IdentifierInfo(BatchTypes.Table, "myTable");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldFindTableReferenceInInsert()
	    {
		    const string sql = "INSERT INTO someTable VALUES(1)";
		    var reference = new IdentifierInfo(BatchTypes.Table, "someTable");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldFindTableReferenceInUpdate()
	    {
		    const string sql = "UPDATE someTable SET Id = 0";
		    var reference = new IdentifierInfo(BatchTypes.Table, "someTable");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldFindTableReferenceInDelete()
	    {
		    const string sql = "DELETE FROM someTable WHERE Id = 0";
		    var reference = new IdentifierInfo(BatchTypes.Table, "someTable");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldNotFindTableReferenceInCreate()
	    {
		    const string sql = "CREATE TABLE someTable (Id INT)";

		    var result = GetReferences(sql);
		    
		    Assert.That(result.Any(), Is.False);
	    }
	    
	    [Test]
	    public void ShouldFindTableReferenceInSubSelect()
	    {
		    const string sql = "SELECT (SELECT TOP 1 Id FROM subTable) AS Id FROM myTable";
		    var reference = new IdentifierInfo(BatchTypes.Table, "subTable");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.Count, Is.EqualTo(2));
		    
		    Assert.That(result[0], Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldFindTableReferenceInWhere()
	    {
		    const string sql = "SELECT 1 AS Id FROM myTable WHERE 1 = (SELECT TOP 1 Id FROM subTable)";
		    var reference = new IdentifierInfo(BatchTypes.Table, "subTable");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.Count, Is.EqualTo(2));
		    
		    Assert.That(result[1], Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldFindTableReferenceInJoin()
	    {
		    const string sql = "SELECT 1 AS Id FROM myTable AS mT JOIN subTable AS sT On mT.Id = sT.Id";
		    var reference = new IdentifierInfo(BatchTypes.Table, "subTable");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.Count, Is.EqualTo(2));
		    
		    Assert.That(result[1], Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldFillDefaultIdentifiers()
	    {
		    const string sql = "SELECT * FROM myTable";
		    var reference = new IdentifierInfo(BatchTypes.Table, "myTable", "someSchema", "someDb", "someServer");

		    var result = GetReferences(sql, "someSchema", "someDb", "someServer");
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldFindNamedTableReferenceInSelect()
	    {
		    const string sql = "SELECT * FROM dbo.myTable";
		    var reference = new IdentifierInfo(BatchTypes.Table, "myTable", "dbo");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldFindNamedTableReferenceWithSchemaDatabase()
	    {
		    const string sql = "SELECT * FROM db1.dbo.myTable";
		    var reference = new IdentifierInfo(BatchTypes.Table, "myTable", "dbo", "db1", null);

		    var result = GetReferences(sql);
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldFindNamedTableReferenceWithSchemaDatabaseServer()
	    {
		    const string sql = "SELECT * FROM server1.db1.dbo.myTable";
		    var reference = new IdentifierInfo(BatchTypes.Table, "myTable", "dbo", "db1", "server1");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
        public void ShouldFindNamedTableReferenceInSelectWithBrackets()
        {
	        const string sql = "SELECT * FROM [server1].[db1].[dbo].[myTable]";            			
			
	        var reference = new IdentifierInfo(BatchTypes.Table, "myTable", "dbo", "db1", "server1");

	        var result = GetReferences(sql);
		    
	        Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
        }
	    
	    [Test]
	    public void ShouldFindProcReferenceInExec()
	    {
		    const string sql = "EXECUTE someProc @someParameter='A'";            			
			
		    var reference = new IdentifierInfo(BatchTypes.Procedure, "someProc");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldFindProcReferenceInCreatedProc()
	    {
		    const string sql = "CREATE PROCEDURE ThisOne AS BEGIN EXECUTE someProc @someParameter='A' END";            			
			
		    var reference = new IdentifierInfo(BatchTypes.Procedure, "someProc");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldFindProcReferenceInAlterProc()
	    {
		    const string sql = "ALTER PROCEDURE ThisOne AS BEGIN EXECUTE someProc @someParameter='A' END";            			
			
		    var reference = new IdentifierInfo(BatchTypes.Procedure, "someProc");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldNotFindProcReferenceInDeleteProc()
	    {
		    const string sql = "DROP PROCEDURE ThisOne";            			
			
		    var result = GetReferences(sql);
		    
		    Assert.That(result.Any(), Is.False);
	    }
	    
	    [Test]
	    public void ShouldFindFuncReferenceInCreatedProc()
	    {
		    const string sql = "CREATE FUNCTION ThisOne() RETURNS INT AS BEGIN RETURN SomeFunc() END";            			
			
		    var reference = new IdentifierInfo(BatchTypes.Function, "SomeFunc");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldFindFuncReferenceInAlterProc()
	    {
		    const string sql = "ALTER FUNCTION ThisOne() RETURNS INT AS BEGIN RETURN SomeFunc() END";            			
			
		    var reference = new IdentifierInfo(BatchTypes.Function, "SomeFunc");

		    var result = GetReferences(sql);
		    
		    Assert.That(result.SingleOrDefault(), Is.EqualTo(reference));
	    }
	    
	    [Test]
	    public void ShouldNotFindFuncReferenceInDeleteFunc()
	    {
		    const string sql = "DROP FUNCTION ThisOne";            			
			
		    var result = GetReferences(sql);
		    
		    Assert.That(result.Any(), Is.False);
	    }
    }
}