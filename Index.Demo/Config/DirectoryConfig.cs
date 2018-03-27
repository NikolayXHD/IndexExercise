using System.Runtime.Serialization;

namespace IndexExercise.Index.Demo
{
	[DataContract]
	public class DirectoryConfig
	{
		[DataMember(Name = "Path")]
		public string Path { get; set; }
	}
}