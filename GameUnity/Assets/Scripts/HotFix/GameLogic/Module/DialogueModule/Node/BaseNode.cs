using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XNode;

namespace GameLogic
{
	public abstract class BaseNode : Node
	{
		// [Title("头像", TitleAlignment = TitleAlignments.Centered, HorizontalLine = false, Bold = true)]
		[HideLabel]
		[PreviewField(80, ObjectFieldAlignment.Center)]
		[SerializeField] private Sprite headIcon;

		[Title("名字", TitleAlignment = TitleAlignments.Centered, HorizontalLine = false, Bold = true)]
		[HideLabel]
		[PropertySpace(SpaceBefore = -8)]
		[SerializeField] private string speakerName;

		[Title("内容", TitleAlignment = TitleAlignments.Centered, HorizontalLine = false, Bold = true)]
		[HideLabel]
		[PropertySpace(SpaceBefore = -8)]
		[TextArea(3, 5)]
		[SerializeField] private string dialogueContent;

		public string HeadLocation => headIcon.name;
		public string SpeakerName => speakerName;
		public string DialogueContent => dialogueContent;


		protected override void Init()
		{
			base.Init();

		}

		// Return the correct value of an output port when requested
		public override object GetValue(NodePort port)
		{
			return null; // Replace this
		}
	}
}