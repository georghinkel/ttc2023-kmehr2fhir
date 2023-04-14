package ttc2023.kmehr2fhir.reference;

import org.eclipse.emf.ecore.resource.Resource;
import org.eclipse.emf.ecore.resource.ResourceSet;
import org.eclipse.emf.ecore.resource.impl.ResourceSetImpl;
import org.eclipse.m2m.atl.emftvm.EmftvmFactory;
import org.eclipse.m2m.atl.emftvm.ExecEnv;
import org.eclipse.m2m.atl.emftvm.Metamodel;
import org.eclipse.m2m.atl.emftvm.Model;
import org.eclipse.m2m.atl.emftvm.impl.resource.EMFTVMResourceFactoryImpl;
import org.eclipse.m2m.atl.emftvm.util.ClassModuleResolver;
import org.eclipse.m2m.atl.emftvm.util.ModuleResolver;
import org.eclipse.m2m.atl.emftvm.util.TimingData;
import org.hl7.emf.fhir.FhirPackage;

import be.fgov.ehealth.standards.kmehr.schema.kmehr.DocumentRoot;
import be.fgov.ehealth.standards.kmehr.schema.kmehr.KmehrPackage;

/**
 * Encapsulates the <code>KMEHRtoFHIR</code> transformation as a single class.
 * To use it, simply create an instance with the appropriate source
 * {@link DocumentRoot} and target {@link Resource}, and run the {@link #run()}
 * method.
 */
public class Transformation {

	private static final String OUT_METAMODEL = "FHIR";
	private static final String IN_METAMODEL = "KMEHR";
	private static final String MODULE_NAME = "KMEHRtoFHIR";
	private final DocumentRoot kmehrRoot;
	private final Resource outputResource;

	public Transformation(final DocumentRoot kmehrRoot, final Resource outputResource) {
		this.kmehrRoot = kmehrRoot;
		this.outputResource = outputResource;
	}

	public DocumentRoot getKmehrRoot() {
		return kmehrRoot;
	}

	public void run() {
		final ExecEnv env = EmftvmFactory.eINSTANCE.createExecEnv();
		final ResourceSet rs = new ResourceSetImpl();

		final Metamodel kmehrMetamodel = EmftvmFactory.eINSTANCE.createMetamodel();
		kmehrMetamodel.setResource(KmehrPackage.eINSTANCE.eResource());
		env.registerMetaModel(IN_METAMODEL, kmehrMetamodel);

		final Metamodel fhirMetamodel = EmftvmFactory.eINSTANCE.createMetamodel();
		fhirMetamodel.setResource(FhirPackage.eINSTANCE.eResource());
		env.registerMetaModel(OUT_METAMODEL, fhirMetamodel);

		// loading models
		rs.getResourceFactoryRegistry().getExtensionToFactoryMap().put("emftvm", new EMFTVMResourceFactoryImpl());

		final Model inModel = EmftvmFactory.eINSTANCE.createModel();
		inModel.setResource(kmehrRoot.eResource());
		env.registerInputModel("IN", inModel);

		final Model outModel = EmftvmFactory.eINSTANCE.createModel();
		outModel.setResource(outputResource);
		env.registerOutputModel("OUT", outModel);

		final ModuleResolver mr = new ClassModuleResolver(Transformation.class);
		final TimingData td = new TimingData();
		env.loadModule(mr, MODULE_NAME);
		td.finishLoading();
		env.run(td);
		td.finish();
	}

	public Resource getOutputResource() {
		return outputResource;
	}
}
