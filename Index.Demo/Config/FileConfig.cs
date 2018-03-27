using System.Runtime.Serialization;

namespace IndexExercise.Index.Demo
{
	[DataContract]
	public class FileConfig
	{
		[DataMember(Name = "Path")]
		public string Path { get; set; }
	}
}