using AppodealAds.Unity.Common;

namespace AppodealAds.Unity.Api
{
	public class UserSettings
	{
		public enum Gender
		{
			OTHER = 0,
			MALE = 1,
			FEMALE = 2
		}

		public enum Occupation
		{
			OTHER = 0,
			WORK = 1,
			SCHOOL = 2,
			UNIVERSITY = 3
		}

		public enum Relation
		{
			OTHER = 0,
			SINGLE = 1,
			DATING = 2,
			ENGAGED = 3,
			MARRIED = 4,
			SEARCHING = 5
		}

		public enum Smoking
		{
			NEGATIVE = 0,
			NEUTRAL = 1,
			POSITIVE = 2
		}

		public enum Alcohol
		{
			NEGATIVE = 0,
			NEUTRAL = 1,
			POSITIVE = 2
		}

		private static IAppodealAdsClient client;

		public UserSettings()
		{
			getInstance().getUserSettings();
		}

		private static IAppodealAdsClient getInstance()
		{
			if (client == null)
			{
				client = AppodealAdsClientFactory.GetAppodealAdsClient();
			}
			return client;
		}

		public UserSettings setUserId(string id)
		{
			getInstance().setUserId(id);
			return this;
		}

		public UserSettings setAge(int age)
		{
			getInstance().setAge(age);
			return this;
		}

		public UserSettings setBirthday(string bDay)
		{
			getInstance().setBirthday(bDay);
			return this;
		}

		public UserSettings setEmail(string email)
		{
			getInstance().setEmail(email);
			return this;
		}

		public UserSettings setGender(Gender gender)
		{
			switch (gender)
			{
			case Gender.OTHER:
				getInstance().setGender(1);
				return this;
			case Gender.MALE:
				getInstance().setGender(2);
				return this;
			case Gender.FEMALE:
				getInstance().setGender(3);
				return this;
			default:
				return null;
			}
		}

		public UserSettings setInterests(string interests)
		{
			getInstance().setInterests(interests);
			return this;
		}

		public UserSettings setOccupation(Occupation occupation)
		{
			switch (occupation)
			{
			case Occupation.OTHER:
				getInstance().setOccupation(1);
				return this;
			case Occupation.WORK:
				getInstance().setOccupation(2);
				return this;
			case Occupation.SCHOOL:
				getInstance().setOccupation(3);
				return this;
			case Occupation.UNIVERSITY:
				getInstance().setOccupation(4);
				return this;
			default:
				return null;
			}
		}

		public UserSettings setRelation(Relation relation)
		{
			switch (relation)
			{
			case Relation.OTHER:
				getInstance().setRelation(1);
				return this;
			case Relation.SINGLE:
				getInstance().setRelation(2);
				return this;
			case Relation.DATING:
				getInstance().setRelation(3);
				return this;
			case Relation.ENGAGED:
				getInstance().setRelation(4);
				return this;
			case Relation.MARRIED:
				getInstance().setRelation(5);
				return this;
			case Relation.SEARCHING:
				getInstance().setRelation(6);
				return this;
			default:
				return null;
			}
		}

		public UserSettings setAlcohol(Alcohol alcohol)
		{
			switch (alcohol)
			{
			case Alcohol.NEGATIVE:
				getInstance().setAlcohol(1);
				return this;
			case Alcohol.NEUTRAL:
				getInstance().setAlcohol(2);
				return this;
			case Alcohol.POSITIVE:
				getInstance().setAlcohol(3);
				return this;
			default:
				return null;
			}
		}

		public UserSettings setSmoking(Smoking smoking)
		{
			switch (smoking)
			{
			case Smoking.NEGATIVE:
				getInstance().setSmoking(1);
				return this;
			case Smoking.NEUTRAL:
				getInstance().setSmoking(2);
				return this;
			case Smoking.POSITIVE:
				getInstance().setSmoking(3);
				return this;
			default:
				return null;
			}
		}
	}
}
