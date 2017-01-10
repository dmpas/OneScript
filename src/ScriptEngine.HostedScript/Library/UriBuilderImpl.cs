/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;
using System;

namespace ScriptEngine.HostedScript.Library
{
	[ContextClass("ПостроительURI", "URIBuilder")]
	public class UriBuilderImpl : AutoContext<UriBuilderImpl>
	{

		private readonly UriBuilder native;

		public UriBuilderImpl()
		{
			native = new UriBuilder();
		}

		public UriBuilderImpl(UriBuilder initialBuilder)
		{
			native = initialBuilder;
		}

		[ContextProperty("Схема", "Scheme")]
		public string Scheme
		{
			get { return native.Scheme; }
			set { native.Scheme = value; }
		}

		[ContextProperty("Хост", "Host")]
		public string Host
		{
			get { return native.Host; }
			set { native.Host = value; }
		}

		[ContextProperty("Порт", "Port")]
		public int Port
		{
			get { return native.Port; }
			set { native.Port = value; }
		}

		[ContextProperty("ИмяПользователя", "UserName")]
		public string UserName
		{
			get { return native.UserName; }
			set { native.UserName = value; }
		}

		[ContextProperty("Пароль", "Password")]
		public string Password
		{
			get { return native.Password; }
			set { native.Password = value; }
		}

		[ContextProperty("Путь", "Path")]
		public string Path
		{
			get { return native.Path; }
			set { native.Path = value; }
		}

		[ContextProperty("Запрос", "Query")]
		public string Query
		{
			get { return native.Query; }
			set { native.Query = value; }
		}

		[ContextProperty("Фрагмент", "Fragment")]
		public string Fragment
		{
			get { return native.Fragment; }
			set { native.Fragment = value; }
		}

		[ContextProperty("УИР", "URI")]
		public string Uri
		{
			get { return native.Uri.ToString(); }
		}

		[ScriptConstructor]
		public static IRuntimeContextInstance Constructor()
		{
			return new UriBuilderImpl();
		}

		[ScriptConstructor]
		public static IRuntimeContextInstance Constructor(IValue uri)
		{
			string initialUri = uri?.AsString() ?? "";
			return new UriBuilderImpl(new UriBuilder(initialUri));
		}

		[ScriptConstructor]
		public static IRuntimeContextInstance Constructor(
			IValue scheme,
			IValue host,
			IValue port = null,
			IValue path = null,
			IValue query = null)
		{
			return new UriBuilderImpl(new UriBuilder(
				scheme?.AsString() ?? "",
				host?.AsString() ?? "",
				(int)(port?.AsNumber() ?? 0),
				path?.AsString() ?? "",
				query?.AsString() ?? ""));
		}
	}
}
