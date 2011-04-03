﻿#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010 FUJIWARA, Yusuke
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
#endregion -- License Terms --

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace MsgPack
{
	[TestFixture]
	public partial class DirectConversionTest
	{
		[Test]
		public void TestNil()
		{
			var output = new MemoryStream();
			Packer.Create( output ).PackNull();
			Assert.AreEqual( null, Unpacking.UnpackNull( new MemoryStream( output.ToArray() ) ) );
			Assert.AreEqual( null, Unpacking.UnpackNull( output.ToArray() ).Value );
		}

		[Test]
		public void TestBoolean()
		{
			TestBoolean( false );
			TestBoolean( true );
		}

		private static void TestBoolean( bool value )
		{
			var output = new MemoryStream();
			Packer.Create( output ).Pack( value );
			Assert.AreEqual( value, Unpacking.UnpackBoolean( new MemoryStream( output.ToArray() ) ) );
			Assert.AreEqual( value, Unpacking.UnpackBoolean( output.ToArray() ).Value );
		}

		[Test]
		public void TestString()
		{
			TestString( "" );
			TestString( "a" );
			TestString( "ab" );
			TestString( "abc" );
			TestString( "\ud9c9\udd31" );
			TestString( "\u30e1\u30c3\u30bb\u30fc\u30b8\u30d1\u30c3\u30af" );

			// continuation
			{
				var ms = new MemoryStream();
				var encoded = Encoding.UTF8.GetBytes( "\u30e1\u30c3\u30bb\u30fc\u30b8\u30d1\u30c3\u30af" );
				Packer.Create( ms ).PackRawHeader( encoded.Length );
				ms.Write( encoded, 0, encoded.Length - 1 );
				ms.Seek( 0, SeekOrigin.Begin );
				Assert.AreEqual( "\u30e1\u30c3\u30bb\u30fc\u30b8\u30d1\u30c3", Unpacking.UnpackString( ms ) );
				var pos = ms.Position;
				var remain = ms.Length - ms.Position;
				ms.Seek( 0, SeekOrigin.End );
				ms.WriteByte( encoded[ encoded.Length - 1 ] );
				ms.Position = pos;
				Assert.AreEqual( "\u30af", Unpacking.UnpackStringBody( ms, 1 + remain ) );
			}

			var sw = Stopwatch.StartNew();
			var avg = 0.0;
			Random random = new Random();
			var sb = new StringBuilder( 1000 * 1000 * 200 );
			// small size string
			for ( int i = 0; i < 100; i++ )
			{
				sb.Length = 0;
				int len = ( int )random.Next() % 31 + 1;
				for ( int j = 0; j < len; j++ )
				{
					sb.Append( 'a' + ( ( int )random.Next() ) & 26 );
				}
				avg = ( avg + sb.Length ) / 2.0;
				TestString( sb.ToString() );
			}
			sw.Stop();
			Console.WriteLine( "Small String ({1:#.0}): {0:0.###} msec/object", sw.ElapsedMilliseconds / 100.0, avg );
			sw.Reset();
			sw.Start();

			avg = 0.0;
			// medium size string
			for ( int i = 0; i < 100; i++ )
			{
				sb.Length = 0;
				int len = ( int )random.Next() % 100 + ( 1 << 15 );
				for ( int j = 0; j < len; j++ )
				{
					sb.Append( 'a' + ( ( int )random.Next() ) & 26 );
				}
				avg = ( avg + sb.Length ) / 2.0;
				TestString( sb.ToString() );
			}
			sw.Stop();
			Console.WriteLine( "Medium String ({1:#.0}): {0:0.###} msec/object", sw.ElapsedMilliseconds / 100.0, avg );
			sw.Reset();
#if !SKIP_LARGE_TEST
			sw.Start();

			avg = 0.0;
			// large size string
			for ( int i = 0; i < 10; i++ )
			{
				sb.Length = 0;
				int len = ( int )random.Next() % 100 + ( 1 << 24 );
				for ( int j = 0; j < len; j++ )
				{
					sb.Append( 'a' + ( ( int )random.Next() ) & 26 );
				}
				avg = ( avg + sb.Length ) / 2.0;
				TestString( sb.ToString() );
			}
			sw.Stop();
			Console.WriteLine( "Large String ({1:#.0}): {0:0.###} msec/object", sw.ElapsedMilliseconds / 10.0 , avg );
			sw.Reset();
#endif

			// Non-ASCII
			avg = 0.0;
			// medium size string
			for ( int i = 0; i < 100; i++ )
			{
				sb.Length = 0;
				int len = ( int )random.Next() % 100 + ( 1 << 8 );
				for ( int j = 0; j < len; j++ )
				{
					sb.Append( Encoding.UTF32.GetChars( BitConverter.GetBytes( random.Next( 0x10ffff ) ) ) );
				}
				avg = ( avg + sb.Length ) / 2.0;
				TestString( sb.ToString() );
			}
			sw.Stop();
			Console.WriteLine( "Medium String ({1:#.0}): {0:0.###} msec/object", sw.ElapsedMilliseconds / 100.0, avg );
		}

		private static void TestString( String value )
		{
			var output = new MemoryStream();
			Packer.Create( output ).PackString( value );
			Assert.AreEqual( value, Unpacking.UnpackString( new MemoryStream( output.ToArray() ) ) );
			Assert.AreEqual( value, Unpacking.UnpackString( output.ToArray() ) );
		}

		[Test]
		public void TestArray()
		{
			var emptyList = new List<int>();
			{
				var output = new MemoryStream();
				Packer.Create( output ).Pack( emptyList );
				Assert.AreEqual( 0, Unpacking.UnpackArrayLength( new MemoryStream( output.ToArray() ) ) );
				Assert.AreEqual( 0, Unpacking.UnpackArrayLength( output.ToArray() ).Value );
			}

			var random = new Random();

			for ( int i = 0; i < 1000; i++ )
			{
				var l = new List<int>();
				int len = ( int )random.Next() % 1000 + 1;
				for ( int j = 0; j < len; j++ )
				{
					l.Add( j );
				}
				var output = new MemoryStream();
				Packer.Create( output ).Pack( l );

				Stream streamInput = new MemoryStream( output.ToArray() );
				Assert.AreEqual( len, Unpacking.UnpackArrayLength( streamInput ) );
				for ( int j = 0; j < len; j++ )
				{
					Assert.AreEqual( l[ j ], Unpacking.UnpackInt32( streamInput ) );
				}

				byte[] byteArrayInput = output.ToArray();
				var arrayLength = Unpacking.UnpackArrayLength( byteArrayInput );
				Assert.AreEqual( len, arrayLength.Value );
				int offset = arrayLength.NewOffset;
				for ( int j = 0; j < len; j++ )
				{
					var uar = Unpacking.UnpackInt32( byteArrayInput, offset );
					Assert.AreNotEqual( offset, uar.NewOffset );
					Assert.AreEqual( l[ j ], uar.Value );
					offset = uar.NewOffset;
				}
			}

			for ( int i = 0; i < 1000; i++ )
			{
				var l = new List<String>();
				int len = ( int )random.Next() % 1000 + 1;
				for ( int j = 0; j < len; j++ )
				{
					l.Add( j.ToString() );
				}
				var output = new MemoryStream();
				Packer.Create( output ).Pack( l );

				Stream streamInput = new MemoryStream( output.ToArray() );
				Assert.AreEqual( len, Unpacking.UnpackArrayLength( streamInput ) );
				for ( int j = 0; j < len; j++ )
				{
					Assert.AreEqual( l[ j ], Unpacking.UnpackString( streamInput ) );
				}

				byte[] byteArrayInput = output.ToArray();
				var arrayLength = Unpacking.UnpackArrayLength( byteArrayInput );
				Assert.AreEqual( len, arrayLength.Value );
				int offset = arrayLength.NewOffset;
				for ( int j = 0; j < len; j++ )
				{
					var uar = Unpacking.UnpackRawLength( byteArrayInput, offset );
					Assert.AreEqual( l[ j ], Unpacking.UnpackString( byteArrayInput, offset ) );
					offset = uar.NewOffset + ( int )uar.Value;
				}
			}
		}

		[Test]
		public void TestMap()
		{
			var emptyMap = new Dictionary<int, int>();
			{
				var output = new MemoryStream();
				Packer.Create( output ).Pack( emptyMap );
				Assert.AreEqual( 0, Unpacking.UnpackDictionaryCount( new MemoryStream( output.ToArray() ) ) );
				Assert.AreEqual( 0, Unpacking.UnpackDictionaryCount( output.ToArray() ).Value );
			}

			var random = new Random();

			for ( int i = 0; i < 1000; i++ )
			{
				var m = new Dictionary<int, int>();
				int len = ( int )random.Next() % 1000 + 1;
				for ( int j = 0; j < len; j++ )
				{
					m[ j ] = j;
				}
				var output = new MemoryStream();
				Packer.Create( output ).Pack( m );

				Stream streamInput = new MemoryStream( output.ToArray() );
				Assert.AreEqual( len, Unpacking.UnpackDictionaryCount( streamInput ) );
				for ( int j = 0; j < len; j++ )
				{
					int value;
					Assert.IsTrue( m.TryGetValue( Unpacking.UnpackInt32( streamInput ), out value ) );
					Assert.AreEqual( value, Unpacking.UnpackInt32( streamInput ) );
				}

				byte[] byteArrayInput = output.ToArray();
				var arrayLength = Unpacking.UnpackDictionaryCount( byteArrayInput );
				Assert.AreEqual( len, arrayLength.Value );
				int offset = arrayLength.NewOffset;
				for ( int j = 0; j < len; j++ )
				{
					var uar = Unpacking.UnpackInt32( byteArrayInput, offset );
					Assert.AreNotEqual( offset, uar.NewOffset );
					int value;
					Assert.IsTrue( m.TryGetValue( uar.Value, out value ) );
					uar = Unpacking.UnpackInt32( byteArrayInput, uar.NewOffset );
					Assert.AreEqual( value, uar.Value );
					offset = uar.NewOffset;
				}
			}

			for ( int i = 0; i < 1000; i++ )
			{
				var m = new Dictionary<string, int>();
				int len = ( int )random.Next() % 1000 + 1;
				for ( int j = 0; j < len; j++ )
				{
					m[ j.ToString() ] = j;
				}
				var output = new MemoryStream();
				Packer.Create( output ).Pack( m );

				Stream streamInput = new MemoryStream( output.ToArray() );
				Assert.AreEqual( len, Unpacking.UnpackDictionaryCount( streamInput ) );
				for ( int j = 0; j < len; j++ )
				{
					int value;
					Assert.IsTrue( m.TryGetValue( Unpacking.UnpackString( streamInput ), out value ) );
					Assert.AreEqual( value, Unpacking.UnpackInt32( streamInput ) );
				}

				byte[] byteArrayInput = output.ToArray();
				var arrayLength = Unpacking.UnpackDictionaryCount( byteArrayInput );
				Assert.AreEqual( len, arrayLength.Value );
				int offset = arrayLength.NewOffset;
				for ( int j = 0; j < len; j++ )
				{
					var length = Unpacking.UnpackRawLength( byteArrayInput, offset );
					Assert.AreNotEqual( offset, length.NewOffset );
					int value;
					Assert.IsTrue( m.TryGetValue( Unpacking.UnpackString( byteArrayInput, offset ), out value ) );
					var uar = Unpacking.UnpackInt32( byteArrayInput, length.NewOffset + ( int )length.Value );
					Assert.AreEqual( value, uar.Value );
					offset = uar.NewOffset;
				}
			}
		}
	}
}
