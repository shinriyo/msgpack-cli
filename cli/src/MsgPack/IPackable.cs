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

namespace MsgPack
{
	/// <summary>
	///		Represents objects which knows how to pack ifself using specified <see cref="Packer"/>.
	/// </summary>
	public interface IPackable
	{
		/// <summary>
		///		Pack this instance itself using specified <see cref="Packer"/>.
		/// </summary>
		/// <param name="packer"><see cref="Packer"/>.</param>
		/// <param name="options">Packing options. This value can be null.</param>
		/// <exception cref="ArgumentNullException"><paramref name="packer"/> is null.</exception>
		void PackToMessage( Packer packer, PackingOptions options );
	}
}
