using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class FrameSpritePool : MonoBehaviour
    {
        public List<Sprite> Idle = new List<Sprite>();
        public List<Sprite> Walk = new List<Sprite>();
        public List<Sprite> Run = new List<Sprite>();
        public List<Sprite> Skill = new List<Sprite>();
        public List<Sprite> Skill1 = new List<Sprite>();
        public List<Sprite> Skill2 = new List<Sprite>();
        public List<Sprite> Skill3 = new List<Sprite>();
        public List<Sprite> Hurt = new List<Sprite>();
        public List<Sprite> Hurt1 = new List<Sprite>();
        public List<Sprite> Death = new List<Sprite>();
        public List<Sprite> Death1 = new List<Sprite>();
        public List<Sprite> Death2 = new List<Sprite>();

        public List<Sprite> GetSprites(FrameAnimName animName)
        {
            List<Sprite> ret = null;
            switch (animName)
            {
                case FrameAnimName.idle:
                    ret = Idle;
                    break;

                case FrameAnimName.idle1:
                    break;

                case FrameAnimName.idle2:
                    break;

                case FrameAnimName.idle3:
                    break;

                case FrameAnimName.idle4:
                    break;

                case FrameAnimName.idle5:
                    break;

                case FrameAnimName.run:
                    ret = Run;
                    break;

                case FrameAnimName.run1:
                    break;

                case FrameAnimName.run2:
                    break;

                case FrameAnimName.run3:
                    break;

                case FrameAnimName.run4:
                    break;

                case FrameAnimName.run5:
                    break;

                case FrameAnimName.attack:
                    break;

                case FrameAnimName.attack1:
                    break;

                case FrameAnimName.attack2:
                    break;

                case FrameAnimName.attack3:
                    break;

                case FrameAnimName.attack4:
                    break;

                case FrameAnimName.attack5:
                    break;

                case FrameAnimName.walk:
                    ret = Walk;
                    break;

                case FrameAnimName.walk1:
                    break;

                case FrameAnimName.walk2:
                    break;

                case FrameAnimName.walk3:
                    break;

                case FrameAnimName.walk4:
                    break;

                case FrameAnimName.walk5:
                    break;

                case FrameAnimName.death:
                    ret = Death;
                    break;

                case FrameAnimName.death1:
                    ret = Death1;
                    break;

                case FrameAnimName.death2:
                    ret = Death2;
                    break;

                case FrameAnimName.death3:
                    break;

                case FrameAnimName.death4:
                    break;

                case FrameAnimName.death5:
                    break;

                case FrameAnimName.appear:
                    break;

                case FrameAnimName.appear1:
                    break;

                case FrameAnimName.appear2:
                    break;

                case FrameAnimName.appear3:
                    break;

                case FrameAnimName.appear4:
                    break;

                case FrameAnimName.appear5:
                    break;

                case FrameAnimName.skill:
                    ret = Skill;
                    break;

                case FrameAnimName.skill1:
                    ret = Skill1;
                    break;

                case FrameAnimName.skill2:
                    ret = Skill2;
                    break;

                case FrameAnimName.skill3:
                    ret = Skill3;
                    break;

                case FrameAnimName.skill4:
                    break;

                case FrameAnimName.skill5:
                    break;

                case FrameAnimName.hurt:
                    ret = Hurt;
                    break;

                case FrameAnimName.hurt1:
                    ret = Hurt1;
                    break;

                case FrameAnimName.hurt2:
                    break;

                case FrameAnimName.hurt3:
                    break;

                case FrameAnimName.hurt4:
                    break;

                case FrameAnimName.hurt5:
                    break;

                case FrameAnimName.loop:
                    break;

                case FrameAnimName.over:
                    break;

                case FrameAnimName.skill_prepare_loop:
                    break;

                case FrameAnimName.behit:
                    break;
            }

            return ret;
        }

        public void AddSprite(FrameAnimName animName, Sprite sprite)
        {
            var list = GetSprites(animName);
            list?.Add(sprite);
        }

        public void SortAllSprites()
        {
            SortSprite(Idle);
            SortSprite(Walk);
            SortSprite(Run);
            SortSprite(Skill);
            SortSprite(Skill1);
            SortSprite(Skill2);
            SortSprite(Skill3);
            SortSprite(Hurt);
            SortSprite(Hurt1);
            SortSprite(Death);
            SortSprite(Death1);
            SortSprite(Death2);
        }

        public void SortSprite(List<Sprite> sprites)
        {
            sprites.Sort((a, b) =>
            {
                int aNum = ParseLastNumber(a.name);
                int bNum = ParseLastNumber(b.name);
                return aNum.CompareTo(bNum);
            });
        }

        private int ParseLastNumber(ReadOnlySpan<char> spriteName)
        {
            int lastUnderscore = spriteName.LastIndexOf('_');
            if (lastUnderscore < 0) return 0;

            var numberSpan = spriteName.Slice(lastUnderscore + 1);
            return int.TryParse(numberSpan, out int result) ? result : 0;
        }
    }
}