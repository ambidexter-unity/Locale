using System;
using Common.GameService;
using Common.Locale;
using UnityEngine;
using Zenject;

namespace Scenes
{
	public class LocaleService : LocaleServiceBase
	{
#pragma warning disable 649
		[Inject] private readonly DiContainer _container;
#pragma warning restore 649

		[Inject]
		// ReSharper disable once UnusedMember.Local
		private void Construct([InjectOptional] SystemLanguage defaultLanguage)
		{
			RestorePersistingState(defaultLanguage);
		}

		protected override string LocalePersistKey => @"test_locale_key";
		protected override string LocalesManifestFileName => @"manifest";
		protected override string LocalesPath => @"Locales";

		protected override bool IsLanguageSupported(SystemLanguage lang)
		{
			switch (lang)
			{
				case SystemLanguage.Russian:
				case SystemLanguage.English:
					// TODO: Add supported languages here.
					return true;
			}

			return false;
		}

		protected override SystemLanguage KeyToLanguage(string key)
		{
			switch (key)
			{
				case "ru_ru": return SystemLanguage.Russian;
				case "en_us": return SystemLanguage.English;
				default:
					throw new NotSupportedException($"Language {key} is not supported.");
			}
		}
	}

	public class TestScript : MonoBehaviour
	{
		void Start()
		{
			var ls = new LocaleService();

			void OnReady(IGameService service)
			{
				ls.ReadyEvent -= OnReady;
				Debug.Log("... stop LocaleService initialized.");

				foreach (var key in _allKeys)
				{
					Debug.Log(ls.GetLocalized(key, SystemLanguage.Russian));
				}
			}

			Debug.Log("Start LocaleService initialized...");
			ls.ReadyEvent += OnReady;
			ls.Initialize();
		}

		private string[] _allKeys =
		{
			"balloon_trim.title",
			"balloon_basket.title",
			"toolbox.title",
			"construction_site.title",
			"bags_with_sand.title",
			"toy_car.title",
			"sphere.title",
			"balloon.title",
			"balloon_trim.description",
			"balloon_basket.description",
			"toolbox.description",
			"construction_site.description",
			"bags_with_sand.description",
			"toy_car.description",
			"sphere.description",
			"balloon.description",
			"tourist_set.title",
			"tourist_set.description",
			"lifesaver_set.title",
			"lifesaver_set.description",
			"raft_ground.title",
			"raft_ground.description",
			"raft_water.title",
			"raft_water.description",
			"expedition.title",
			"expedition.description",
			"bn.read",
			"bn.pause",
			"bn.skip",
			"bn.next",
			"bn.ok",
			"bn.cancel",
			"bn.yes",
			"bn.no",
			"bn.take",
			"bn.collections",
			"bn.wallet",
			"bn.assemble",
			"bn.continue",
			"bn.play",
			"level.complete",
			"stepashka.name",
			"filya.name",
			"karkusha.name",
			"hrusha.name",
			"key",
			"Findpairs.firstLevel",
			"Findpairs.secondLevel",
			"Findpairs.thirdLevel",
			"map.name",
			"map.description",
			"paper.name",
			"balloon_drawing.name",
			"balloon_drawing.description",
			"paper.description",
			"canvas.name",
			"canvas.description",
			"glue.name",
			"glue.description",
			"sheathing.name",
			"sheathing.description",
			"phone.name",
			"phone.description",
			"phonebook.name",
			"phonebook.description",
			"teapot.name",
			"teapot.description",
			"cups.name",
			"cups.description",
			"tablecloth.name",
			"tablecloth.description",
			"twigs.name",
			"twigs.description",
			"rope.name",
			"rope.description",
			"basket.name",
			"basket.description",
			"nippers.name",
			"nippers.description",
			"hammer.name",
			"hammer.description",
			"scissors.name",
			"scissors.description",
			"toolbox.name",
			"toolbox.description",
			"primus.name",
			"primus.description",
			"bags.name",
			"bags.description",
			"needle_with_thread.name",
			"needle_with_thread.description",
			"wheels.name",
			"wheels.description",
			"lighter.name",
			"lighter.description",
			"battery.name",
			"battery.description",
			"brush.name",
			"brush.description",
			"bucket.name",
			"bucket.description",
			"scoop.name",
			"scoop.description",
			"toy_dump_truck_body.name",
			"toy_dump_truck_body.description",
			"toy_dump_truck_cabin.name",
			"toy_dump_truck_cabin.description",
			"bags_with_sand.name",
			"bags_with_sand.description",
			"canister.name",
			"canister.description",
			"rope_ladder.name",
			"rope_ladder.description",
			"building_yard.name",
			"building_yard.description",
			"toy_dump_truck.name",
			"toy_dump_truck.description",
			"sphere.name",
			"sphere.description",
			"balloon.name",
			"balloon.description",
			"beeding.name",
			"beeding.description",
			"cauldron.name",
			"cauldron.description",
			"comb.name",
			"comb.description",
			"compass.name",
			"compass.description",
			"flashlight.name",
			"flashlight.description",
			"hat.name",
			"hat.description",
			"lifebuoy.name",
			"lifebuoy.description",
			"log.name",
			"log.description",
			"logs.name",
			"logs.description",
			"medkit.name",
			"medkit.description",
			"mirror.name",
			"mirror.description",
			"notebook.name",
			"notebook.description",
			"paddle1.name",
			"paddle1.description",
			"paddle2.name",
			"paddle2.description",
			"pencil.name",
			"pencil.description",
			"rope2.name",
			"rope2.description",
			"tent.name",
			"tent.description",
			"lifesaver_set.name",
			"lifesaver_set.description",
			"raft_ground.name",
			"raft_ground.description",
			"raft_water.name",
			"raft_water.description",
			"tourist_set.name",
			"tourist_set.description",
			"expedition.name",
			"expedition.description",
			"tutor.selector1",
			"tutor.selector2",
			"tutor.enter.stepashka.house1",
			"tutor.enter.stepashka.house2",
			"tutor.hog1",
			"tutor.hog2",
			"tutor.hog3",
			"tutor.hog4",
			"tutor.next",
			"tutor.inventory1",
			"tutor.inventory2",
			"tutor.collections1",
			"tutor.collections2",
			"tutor.collections3",
			"tutor.collections4",
			"tutor.collections5",
			"tutor.minigame1",
			"sound.toggle",
			"music.toggle",
			"language.title",
			"lang.rus",
			"new_collection.title",
			"new_collection.description1",
			"new_collection.description2",
			"collections.title",
			"wallet.title",
			"confirm_text.exit",
			"minigame.win",
			"minigame.lose",
			"minigame.lose_description",
			"notification_text.cap",
			"notification_text.pairs_no_tries",
			"new.thing",
			"tips_are_over.message",
			"reset_progress.message"
		};
	}
}