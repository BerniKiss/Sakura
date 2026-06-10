using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace naprendszer {
	internal class PlanetArrangementModel {

		private float forgasiSebessegNapKorul = 0.1f;
		private float forgasiSebessegSajatTengelyeKorul = 0.1f;

		// Paraméterek a pálya nagytengelyéhez és kistengelyéhez
		private float nagytengely = 0f; // Nagytengely fél hossza az X tengely mentén
		private float kistengely = 0f; // Kistengely fél hossza az Z tengely mentén

		public PlanetArrangementModel(float forgasiSebessegNapKorul, float forgasiSebessegSajatTengelyeKorul, float nagytengelyXmenten, float kistengelyZmenten) {
			this.forgasiSebessegNapKorul = forgasiSebessegNapKorul;
			this.forgasiSebessegSajatTengelyeKorul = forgasiSebessegSajatTengelyeKorul;
			this.nagytengely = nagytengelyXmenten;
			this.kistengely = kistengelyZmenten;
			KeringesiPalyaX = nagytengelyXmenten;
			KeringesiPalyaZ = kistengelyZmenten;
		}
		public bool AnimationEnabled { get; set; } = false;

		// ido segit a szimulacio szamitasban
		private double Time { get; set; } = 0;

		public double ForgasiSzogeSajatTengelyeKorulY { get; private set; } = 0;

		/// <summary>
		/// The angle with which the diamond cube is rotated around the diagonal from bottom right front to top left back.
		/// </summary>
		public double BolygoForgasiSzogeGlobalY { get; private set; } = 0;

		public float KeringesiPalyaX { get; private set; } = 0;

		public float KeringesiPalyaZ { get; private set; } = 0;

		internal void AdvanceTime(double deltaTime) {
			// set a simulation time
			Time += deltaTime;

			// we do not advance the simulation when animation is stopped
			if (!AnimationEnabled)
				return;

			ForgasiSzogeSajatTengelyeKorulY += (Time * forgasiSebessegSajatTengelyeKorul) / 1000;

			BolygoForgasiSzogeGlobalY -= (-Time * forgasiSebessegNapKorul) / 5000;

			KeringesiPalyaX = nagytengely * (float)Math.Cos(BolygoForgasiSzogeGlobalY); // Az X pozíció kiszámítása
			KeringesiPalyaZ = kistengely *  (float)Math.Sin(BolygoForgasiSzogeGlobalY); // Az Y pozíció kiszámítása
		}

	}
}
