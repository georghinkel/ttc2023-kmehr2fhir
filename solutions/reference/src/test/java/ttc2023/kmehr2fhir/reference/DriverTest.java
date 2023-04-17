package ttc2023.kmehr2fhir.reference;

import java.io.File;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class DriverTest {

	private static final String MODELS_SUMEHR_EXAMPLE_FHIR = "../../models/sumehr_example.fhir";
	private static final String MODELS_SUMEHR_EXAMPLE_KMEHR = "../../models/sumehr_example.kmehr";

	private Driver driver;

	@BeforeEach
	void setUp() throws Exception {
		final File fKmehr = new File(MODELS_SUMEHR_EXAMPLE_KMEHR);
		final File fFhir = new File(MODELS_SUMEHR_EXAMPLE_FHIR);
		driver = new Driver(fKmehr, fFhir);
	}

	@Test
	void testExecute() throws Exception {
		driver.execute();
	}

	@Test
	void testMain() {
		Driver.main(new String[] { MODELS_SUMEHR_EXAMPLE_KMEHR, MODELS_SUMEHR_EXAMPLE_FHIR });
	}

}
