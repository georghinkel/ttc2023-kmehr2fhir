package ttc2023.kmehr2fhir.reference;

import java.io.File;
import java.io.IOException;

import org.eclipse.emf.common.util.URI;
import org.eclipse.emf.ecore.resource.Resource;
import org.eclipse.emf.ecore.resource.ResourceSet;
import org.eclipse.emf.ecore.resource.impl.ResourceSetImpl;
import org.eclipse.emf.ecore.xmi.impl.EcoreResourceFactoryImpl;
import org.eclipse.emf.ecore.xmi.impl.XMIResourceFactoryImpl;
import org.hl7.emf.fhir.FhirPackage;
import org.hl7.emf.fhir.util.FhirResourceFactoryImpl;

import be.fgov.ehealth.standards.kmehr.schema.kmehr.DocumentRoot;
import be.fgov.ehealth.standards.kmehr.schema.kmehr.KmehrPackage;
import be.fgov.ehealth.standards.kmehr.schema.kmehr.util.KmehrResourceFactoryImpl;

/**
 * This class is not part of any solutions: it's just a utility for running the
 * original transformation that produces the reference document, which is later
 * mutated.
 */
public class Driver {

	private final File fKmehr, fFhir;
	private DocumentRoot kmehrFile;
	private Resource outputResource;
	private String tool;
	private String runIndex;

	public Driver(final File fKmehr, final File fFhir) {
		this.fKmehr = fKmehr;
		this.fFhir = fFhir;
	}

	public static void main(final String[] args) {
		if (args.length != 2) {
			System.err.println("Usage: java -jar ... in.kmehr out.fhir");
			System.exit(1);
		}

		final File fKmehr = new File(args[0]);
		final File fFhir = new File(args[1]);
		final Driver driver = new Driver(fKmehr, fFhir);
		try {
			driver.execute();
		} catch (final Exception e) {
			e.printStackTrace();
		}
	}

	public void execute() throws Exception {
		initialize();
		load();
		run();
	}

	private ResourceSet repository;
	private Transformation solution;
	private long stopwatch;

	@SuppressWarnings("unchecked")
	private <T> T loadFile(final File file) {
		Resource mRes;
		try {
			mRes = repository.getResource(URI.createFileURI(file.getCanonicalPath()), true);
		} catch (final IOException e) {
			throw new RuntimeException(e);
		}
		return (T) mRes.getContents().get(0);
	}

	private void load() throws IOException {
		stopwatch = System.nanoTime();
		kmehrFile = loadFile(fKmehr);

		final URI uri = URI.createFileURI(fFhir.getCanonicalPath());
		outputResource = repository.createResource(uri);
		outputResource.getContents().clear();

		solution = new Transformation(kmehrFile, outputResource);

		stopwatch = System.nanoTime() - stopwatch;
		report(BenchmarkPhase.Load);
	}

	private void initialize() throws Exception {
		tool = System.getenv("Tool");
		runIndex = System.getenv("RunIndex");

		stopwatch = System.nanoTime();

		repository = new ResourceSetImpl();
		repository.getResourceFactoryRegistry().getExtensionToFactoryMap().put("ecore", new EcoreResourceFactoryImpl());
		repository.getResourceFactoryRegistry().getExtensionToFactoryMap().put("*", new XMIResourceFactoryImpl());
		repository.getResourceFactoryRegistry().getExtensionToFactoryMap().put("kmehr",
				new KmehrResourceFactoryImpl());
		repository.getResourceFactoryRegistry().getExtensionToFactoryMap().put("fhir", new FhirResourceFactoryImpl());
		repository.getPackageRegistry().put(KmehrPackage.eINSTANCE.getNsURI(), KmehrPackage.eINSTANCE);
		repository.getPackageRegistry().put(FhirPackage.eINSTANCE.getNsURI(), FhirPackage.eINSTANCE);

		stopwatch = System.nanoTime() - stopwatch;
		report(BenchmarkPhase.Initialization);
	}

	private void run() throws IOException {
		stopwatch = System.nanoTime();

		solution.run();

		stopwatch = System.nanoTime() - stopwatch;
		report(BenchmarkPhase.Run);
		outputResource.save(null);
	}

	private void report(final BenchmarkPhase phase) {
		if ("true".equals(System.getenv("NoGC"))) {
			// nothing to do
		} else {
			Runtime.getRuntime().gc();
			Runtime.getRuntime().gc();
			Runtime.getRuntime().gc();
			Runtime.getRuntime().gc();
			Runtime.getRuntime().gc();
		}

		final long memoryUsed = Runtime.getRuntime().totalMemory() - Runtime.getRuntime().freeMemory();
		if (outputResource != null && !outputResource.getContents().isEmpty()) {
			final org.hl7.emf.fhir.DocumentRoot documentRoot = (org.hl7.emf.fhir.DocumentRoot) outputResource
					.getContents().get(0);
			System.out.println(String.format("%s;%s;%s;%s;%s;%s;%s", tool, fKmehr.getName(), fFhir.getName(), runIndex,
					phase.toString(), "Entries", Integer.toString(documentRoot.getBundle().getEntry().size())));
		}
		System.out.println(
				String.format("%s;%s;%s;%s;%s;%s;%s", tool, fKmehr.getName(), fFhir.getName(), runIndex,
						phase.toString(), "Runtime (ns)", Long.toString(stopwatch)));
		System.out.println(String.format("%s;%s;%s;%s;%s;%s;%s", tool, fKmehr.getName(), fFhir.getName(), runIndex,
				phase.toString(), "Memory used (b)", Long.toString(memoryUsed)));
	}

	enum BenchmarkPhase {
		Initialization, Load, Run
	}
}
